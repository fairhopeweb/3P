﻿using System;
using System.Diagnostics;
using System.Drawing;
using _3PA.MainFeatures.Parser;

namespace _3PA.MainFeatures.SynthaxHighlighting {

    /// <summary>
    /// This class handles the STYLENEEDED notification of scintilla
    /// </summary>
    public class Highlight {
        public static int derp = 0;

        /// <summary>
        /// Called on STYLENEEDED notification
        /// </summary>
        /// <param name="endPos"></param>
        public static void Colorize(int startPos, int endPos) {
            //------------
            var watch = Stopwatch.StartNew();
            //------------

            // redefine the styles
            SetCustomStyles();

            Lexer tok = new Lexer(Npp.GetDocumentText());
            tok.Tokenize();
            SynthaxHighlightVisitor vis = new SynthaxHighlightVisitor {
                FromLine = Npp.GetLineFromPosition(startPos),
                ToLine = Npp.GetLineFromPosition(endPos)
            };
            tok.Accept(vis);

            //--------------
            watch.Stop();
            Npp.SetStatusbarLabel("derp = " + derp + "startPos = " + startPos + ", endPos = " + endPos + ", done in " + watch.ElapsedMilliseconds + " ms");
            //------------
            derp++;
        }

        public static void SetCustomStyles() {
            Npp.SetDefaultStyle(Color.White, Color.Crimson);
            Npp.SetStyle((int)UdlStyles.Default, Color.AntiqueWhite, Color.MidnightBlue);
            Npp.SetStyle((int)UdlStyles.Comment, Color.GreenYellow, Color.Green);
            Npp.SetStyle((int)UdlStyles.CommentLine, Color.Black, Color.Aquamarine);
            Npp.SetStyle((int)UdlStyles.Delimiter1, Color.White, Color.Crimson);
            Npp.SetStyle((int)UdlStyles.Delimiter2, Color.White, Color.Brown);
            Npp.SetStyle((int)UdlStyles.KeyWordsList1, Color.White, Color.DarkViolet);
        }

        /// <summary>
        /// Is the caret not in : an include, a string, a comment
        /// </summary>
        /// <returns></returns>
        public static bool IsCarretInNormalContext(int curPos) {
            try {
                var curContext = (UdlStyles)Npp.GetStyleAt(curPos);
                if (curPos <= 0) return true;
                if (IsNormalContext(curContext)) return true;
                var prevContext = (UdlStyles) Npp.GetStyleAt(curPos - 1);
                return IsNormalContext(prevContext);
            } catch (Exception) {
                // we can be here if the style ID isn't in the UdlStyles enum
                return true;
            }
        }

        /// <summary>
        /// Is the caret not in : an include, a string, a comment
        /// </summary>
        /// <returns></returns>
        public static bool IsNormalContext(UdlStyles context) {
            return (context != UdlStyles.Comment
                    && context != UdlStyles.Delimiter1
                    && context != UdlStyles.Delimiter2
                    && context != UdlStyles.Delimiter3
                    && context != UdlStyles.CommentLine
                    && context != UdlStyles.Delimiter8);
        }
    }

    /// <summary>
    /// Enumeration of the style id used by the UDL
    /// </summary>
    public enum UdlStyles {
        Default = 0,
        Comment = 1,
        CommentLine = 2,
        Number = 3,
        KeyWordsList1 = 4,
        KeyWordsList2 = 5,
        KeyWordsList3 = 6,
        KeyWordsList4 = 7,
        KeyWordsList5 = 8,
        KeyWordsList6 = 9,
        KeyWordsList7 = 10,
        KeyWordsList8 = 11,
        FolderInCode1 = 13,
        FolderInCode2 = 14,
        FolderInComment = 15,
        Delimiter1 = 16,
        Delimiter2 = 17,
        Delimiter3 = 18,
        Delimiter4 = 19,
        Delimiter5 = 20,
        Delimiter6 = 21,
        Delimiter7 = 22,
        Delimiter8 = 23,
        Operators = 24,
    }
}
