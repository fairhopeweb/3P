﻿#region header
// ========================================================================
// Copyright (c) 2016 - Julien Caillon (julien.caillon@gmail.com)
// This file (ProEnvironment.cs) is part of 3P.
// 
// 3P is a free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// 3P is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with 3P. If not, see <http://www.gnu.org/licenses/>.
// ========================================================================
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using _3PA.Lib;

namespace _3PA.MainFeatures.ProgressExecutionNs {

    internal class ProEnvironment {

        #region ProEnvironmentObject

        internal class ProEnvironmentObject {

            #region Exported fields

            // prim key
            public string Name = "";
            public string Suffix = "";

            // label
            public string Label = "";

            // pf
            public Dictionary<string, string> DbConnectionInfo = new Dictionary<string, string>();
            public string ExtraPf = "";

            // propath
            public string IniPath = "";
            public string ExtraProPath = "";

            public string CmdLineParameters = "";

            /// <summary>
            /// Path to the workarea, we can find the .p, .t, .w there
            /// </summary>
            public string BaseLocalPath = "";

            public string BaseCompilationPath = "";
            public bool CompileLocally;

            public string ProwinPath = "";
            public string LogFilePath = "";

            #endregion

            #region constructor

            public ProEnvironmentObject() {
                // need to erase the stored ProPath (and re-compute when needed) when the current environment is modified
                OnEnvironmentChange += ReComputeProPath;

                // we need to filter/sort the list of computation path when it changes
                CompilationPath.OnCompilationPathUpdate += () => _compilationPathList = null;
            }

            /// <summary>
            /// To create a hard copy of this object
            /// </summary>
            public ProEnvironmentObject(ProEnvironmentObject toCopy) {
                Name = toCopy.Name;
                Suffix = toCopy.Suffix;
                Label = toCopy.Label;
                ExtraPf = toCopy.ExtraPf;
                IniPath = toCopy.IniPath;
                ExtraProPath = toCopy.ExtraProPath;
                CmdLineParameters = toCopy.CmdLineParameters;
                BaseLocalPath = toCopy.BaseLocalPath;
                BaseCompilationPath = toCopy.BaseCompilationPath;
                CompileLocally = toCopy.CompileLocally;
                ProwinPath = toCopy.ProwinPath;
                LogFilePath = toCopy.LogFilePath;
                _currentProPathDirList = toCopy._currentProPathDirList;
                _compilationPathList = toCopy._compilationPathList;
            }

            #endregion

            #region Handle pf

            /// <summary>
            /// Returns the currently selected database's .pf for the current environment
            /// </summary>
            public string GetPfPath() {
                return DbConnectionInfo.ContainsKey(Config.Instance.EnvDatabase) ?
                    DbConnectionInfo[Config.Instance.EnvDatabase] :
                    string.Empty;
            }

            public bool RemoveCurrentPfPath() {
                if (DbConnectionInfo.ContainsKey(Config.Instance.EnvDatabase)) {
                    DbConnectionInfo.Remove(Config.Instance.EnvDatabase);
                    return true;
                }
                return false;
            }

            public bool AddPfPath(string name, string path) {
                if (!DbConnectionInfo.ContainsKey(name)) {
                    DbConnectionInfo.Add(name, path);
                    return true;
                }
                return false;
            }

            #endregion

            #region Get ProPath

            private List<string> _currentProPathDirList;

            /// <summary>
            /// Call this method to compute the propath again the next time we call GetProPathFileList
            /// </summary>
            public void ReComputeProPath() {
                _currentProPathDirList = null;
                _savedFoundFiles.Clear();
            }

            /// <summary>
            /// List the existing directories as they are listed in the .ini file + in the custom ProPath field,
            /// this returns an exhaustive list of EXISTING folders and .pl files and ensure each item is present only once
            /// It also take into account the relative path, using the BaseLocalPath (or currentFileFolder)
            /// </summary>
            public List<string> GetProPathDirList {
                get {
                    if (_currentProPathDirList == null) {
                        var curFilePath = Npp.GetCurrentFileFolder();
                        var basePath = (!string.IsNullOrEmpty(BaseLocalPath) && Directory.Exists(BaseLocalPath)) ? BaseLocalPath : curFilePath;

                        // get full propath (from .ini + from user custom field + current file folder)
                        IniReader ini = new IniReader(IniPath);
                        var completeProPath = ini.GetValue("PROPATH", "");
                        completeProPath = curFilePath + "," + completeProPath + "," + ExtraProPath;

                        var uniqueDirList = new HashSet<string>();
                        foreach (var item in completeProPath.Split(',', '\n', ';')) {
                            var propath = item.Trim();
                            if (!string.IsNullOrEmpty(propath)) {
                                // need to take into account relative paths
                                if (!Path.IsPathRooted(propath))
                                    try {
                                        propath = Path.GetFullPath(Path.Combine(basePath, propath));
                                    } catch (Exception x) {
                                        ErrorHandler.Log(x.Message);
                                    }
                                if (Directory.Exists(propath) || File.Exists(propath))
                                    if (!uniqueDirList.Contains(propath))
                                        uniqueDirList.Add(propath);
                            }
                        }
                        _currentProPathDirList = uniqueDirList.ToList();
                    }
                    return _currentProPathDirList;
                }
            }

            #endregion

            #region Find file

            /// <summary>
            /// Finding files in directories is actually a task that can take a long time,
            /// if we get a match, we save it here so the next time we look for the file,
            /// we already know its full path
            /// </summary>
            private Dictionary<string, string> _savedFoundFiles = new Dictionary<string, string>(StringComparer.CurrentCultureIgnoreCase);

            /// <summary>
            /// tries to find the specified file in the current propath
            /// returns an empty string if nothing is found, otherwise returns the fullpath of the file
            /// </summary>
            public string FindFirstFileInPropath(string fileName) {

                if (_savedFoundFiles.ContainsKey(fileName))
                    return _savedFoundFiles[fileName];

                try {
                    foreach (var item in GetProPathDirList) {
                        var curPath = Path.Combine(item, fileName);
                        if (File.Exists(curPath)) {
                            _savedFoundFiles.Add(fileName, curPath);
                            return curPath;
                        }
                    }
                } catch (Exception) {
                    // The path in invalid, well we don't really care
                }
                return "";
            }

            /// <summary>
            /// Find a file in the propath and if it can't find it, in the env base local path
            /// </summary>
            public string FindFirstFileInEnv(string fileToFind) {

                if (_savedFoundFiles.ContainsKey(fileToFind))
                    return _savedFoundFiles[fileToFind];

                // find in propath
                var propathRes = FindFirstFileInPropath(fileToFind);
                if (!string.IsNullOrEmpty(propathRes))
                    return propathRes;

                // find in local files
                var listFilePathLoc = FindAllFiles(BaseLocalPath, fileToFind);
                if (listFilePathLoc.Any()) {
                    _savedFoundFiles.Add(fileToFind, listFilePathLoc.First());
                    return listFilePathLoc.First();
                }

                return "";
            }

            /// <summary>
            /// Returns the fullpath of all the files with the name fileName present either
            /// in the propath (they would be on top of the list) or in the environnement local
            /// base path
            /// You can specify comma separated extensions (ex: .p,.w,.i,.lst) and specifiy an extension-less
            /// fileName to match several files
            /// </summary>
            public List<string> FindFiles(string fileName, string extensions = null) {
                var output = new List<string>();
                try {
                    // search in propath
                    if (string.IsNullOrEmpty(extensions)) {
                        var propathFile = FindFirstFileInPropath((fileName));
                        if (!string.IsNullOrEmpty(propathFile))
                            output.Add(propathFile);
                    } else {
                        output.AddRange(extensions.Split(',').Select(s => FindFirstFileInPropath(fileName + s)).Where(s => !string.IsNullOrEmpty(s)).ToList());
                    }

                    // search in local folder, do not add the same file twice
                    output.AddRange(FindAllFiles(BaseLocalPath, fileName, extensions).Where(file => !output.Contains(file, StringComparer.CurrentCultureIgnoreCase)));
                } catch (Exception x) {
                    if (!(x is DirectoryNotFoundException))
                        ErrorHandler.Log(x.Message);
                }
                return output;
            }

            /// <summary>
            /// Returns the fullpath of all files named fileName in the dirPath
            /// You can specify comma separated extensions (ex: .p,.w,.i,.lst) and specifiy an extension-less
            /// fileName to match several files
            /// </summary>
            public List<string> FindAllFiles(string dirPath, string fileName, string extensions = null) {
                var output = new List<string>();
                try {
                    if (!Directory.Exists(dirPath))
                        return output;
                    if (string.IsNullOrEmpty(extensions)) {
                        output.AddRange(Directory.EnumerateFiles(dirPath, fileName, SearchOption.AllDirectories));
                    } else {
                        output.AddRange(extensions.Split(',').SelectMany(s => Directory.EnumerateFiles(dirPath, fileName + s, SearchOption.AllDirectories)).ToList());
                    }
                } catch (Exception x) {
                    if (!(x is DirectoryNotFoundException))
                        ErrorHandler.Log(x.Message);
                }
                return output;
            }

            /// <summary>
            /// Use this method to know if the CONNECT define for the current environment connects the database in
            /// single user mode (returns false if not or if no database connection is set)
            /// </summary>
            /// <returns></returns>
            public bool IsDatabaseSingleUser() {
                bool singleUserMode = false;

                if (!string.IsNullOrEmpty(ExtraPf)) {
                    if (ExtraPf.MatchRegex(@"\s-1", RegexOptions.Singleline))
                        singleUserMode = true;
                }

                if (!string.IsNullOrEmpty(GetPfPath()) && File.Exists(GetPfPath())) {
                    if (File.ReadAllText(GetPfPath(), Encoding.Default).MatchRegex(@"\s-1", RegexOptions.Singleline))
                        singleUserMode = true;
                }

                return singleUserMode;
            }

            #endregion

            #region CompilationPath

            private List<CompilationPath> _compilationPathList;

            private List<CompilationPath> GetCompilationPathList {
                get {
                    if (_compilationPathList == null) {
                        // where (appli is "" or (appli is currentAppli and (envletter is currentEnvletter or envletter = "")))
                        _compilationPathList = CompilationPath.GetCompilationPathList.Where(
                            item => string.IsNullOrWhiteSpace(item.ApplicationFilter) || (item.ApplicationFilter.EqualsCi(Name) && (item.EnvLetterFilter.EqualsCi(Suffix) || string.IsNullOrWhiteSpace(item.EnvLetterFilter)))
                        ).ToList();

                        // sort, null or space appli/suffix last
                        _compilationPathList.Sort((item1, item2) => {
                            int compare = string.IsNullOrWhiteSpace(item1.ApplicationFilter).CompareTo(string.IsNullOrWhiteSpace(item2.ApplicationFilter));
                            if (compare != 0) return compare;
                            compare = string.IsNullOrWhiteSpace(item1.EnvLetterFilter).CompareTo(string.IsNullOrWhiteSpace(item2.EnvLetterFilter));
                            return compare;
                        });
                    }
                    return _compilationPathList;
                }
            }

            /// <summary>
            /// This method returns the compilation directory for the given source path
            /// If CompileLocally, returns the directory of the source
            /// If the base compilation is empty and we didn't match an absolute compilation path, returns the source directoy as well
            /// </summary>
            public string GetCompilationDirectory(string sourcePath) {

                // local compilation?
                if (CompileLocally)
                    return Path.GetDirectoryName(sourcePath);

                var baseComp = BaseCompilationPath;

                // try to find the first item that match the input pattern
                if (GetCompilationPathList.Count > 0) {
                    var canFind = GetCompilationPathList.FirstOrDefault(item => sourcePath.MatchRegex(item.InputPathPattern.WildCardToRegex()));
                    if (canFind != null) {
                        if (Path.IsPathRooted(canFind.OutputPathAppend)) {
                            baseComp = canFind.OutputPathAppend;
                        } else {
                            if (!string.IsNullOrEmpty(baseComp))
                                baseComp = Path.Combine(baseComp, canFind.OutputPathAppend);
                        }
                    }
                }

                return string.IsNullOrEmpty(baseComp) ? Path.GetDirectoryName(sourcePath) : baseComp;
            }

            #endregion

        }

        #endregion


        #region events

        /// <summary>
        /// Subscribe to this event, published when the current environment changes
        /// </summary>
        public static event Action OnEnvironmentChange;

        #endregion

        #region fields

        private static ProEnvironmentObject _currentEnv;
        private static List<ProEnvironmentObject> _listOfEnv = new List<ProEnvironmentObject>();

        #endregion

        #region public manage env

        /// <summary>
        /// To call when the .xml file has changed and you want to reload it
        /// </summary>
        public static void Import() {
            _listOfEnv.Clear();
        }

        /// <summary>
        /// Returns the list of all the progress envrionnements configured
        /// </summary>
        /// <returns></returns>
        public static List<ProEnvironmentObject> GetList {
            get {
                if (_listOfEnv.Count == 0) {
                    if (!File.Exists(Config.FileProEnv)) {
                        _listOfEnv = new List<ProEnvironmentObject> { new ProEnvironmentObject { Name = "Default", Label = "A default environment (empty)" } };
                    } else
                        Object2Xml<ProEnvironmentObject>.LoadFromFile(_listOfEnv, Config.FileProEnv);
                }
                return _listOfEnv;
            }
        }

        /// <summary>
        /// Saves the list of environnement
        /// </summary>
        public static void SaveList() {
            // sort by appli then envletter
            _listOfEnv.Sort((env1, env2) => {
                var comp = string.Compare(env1.Name, env2.Name, StringComparison.CurrentCultureIgnoreCase);
                return comp == 0 ? string.Compare(env1.Suffix, env2.Suffix, StringComparison.CurrentCultureIgnoreCase) : comp;
            });
            if (!string.IsNullOrEmpty(Config.FileProEnv)) {
                try {
                    Object2Xml<ProEnvironmentObject>.SaveToFile(_listOfEnv, Config.FileProEnv);
                } catch (Exception e) {
                    ErrorHandler.ShowErrors(e, "Error when saving ProgressEnvironnement.xml");
                }
            }
        }

        /// <summary>
        /// Saves an environment either by creating a new one (before == null) or 
        /// replacing an old one
        /// </summary>
        /// <param name="before"></param>
        /// <param name="after"></param>
        public static void Modify(ProEnvironmentObject before, ProEnvironmentObject after) {
            if (before != null) {
                var index = _listOfEnv.FindIndex(environnement =>
                    environnement.Name.EqualsCi(before.Name) &&
                    environnement.Suffix.EqualsCi(before.Suffix));
                if (index > -1) {
                    _listOfEnv.RemoveAt(index);
                }
            }
            _listOfEnv.Add(after);
            SetCurrent(null, null, null);
        }

        /// <summary>
        /// Deletes the current environment from the list
        /// </summary>
        public static void DeleteCurrent() {
            var index = _listOfEnv.FindIndex(environnement =>
                environnement.Name.EqualsCi(Current.Name) &&
                environnement.Suffix.EqualsCi(Current.Suffix));
            if (index > -1) {
                _listOfEnv.RemoveAt(index);
            }
            SetCurrent(null, null, null);
        }

        /// <summary>
        /// Return the current ProgressEnvironnement object (null if the list is empty!)
        /// </summary>
        public static ProEnvironmentObject Current {
            get {
                if (_currentEnv == null)
                    SetCurrent(null, null, null);
                return _currentEnv;
            }
        }

        /// <summary>
        /// Change the current environment
        /// </summary>
        public static void SetCurrent(string name, string suffix, string database) {

            // determines the current item selected in the envList
            var envList = GetList;
            _currentEnv = envList.FirstOrDefault(environnement =>
                environnement.Name.EqualsCi(name ?? Config.Instance.EnvName) &&
                environnement.Suffix.EqualsCi(suffix ?? Config.Instance.EnvSuffix));
            if (_currentEnv == null) {
                _currentEnv = envList.FirstOrDefault(environnement =>
                    environnement.Name.EqualsCi(name ?? Config.Instance.EnvName));
            }
            if (_currentEnv == null) {
                _currentEnv = envList.Count > 0 ? envList[0] : new ProEnvironmentObject();
            }

            Config.Instance.EnvName = _currentEnv.Name;
            Config.Instance.EnvSuffix = _currentEnv.Suffix;

            // set database
            if (!_currentEnv.DbConnectionInfo.ContainsKey(database ?? Config.Instance.EnvDatabase))
                Config.Instance.EnvDatabase = (_currentEnv.DbConnectionInfo.Count > 0) ? _currentEnv.DbConnectionInfo.First().Key : String.Empty;

            if (OnEnvironmentChange != null)
                OnEnvironmentChange();
        }

        #endregion

    }

    
}
