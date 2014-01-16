using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using EnvDTE80;
using EnvDTE;

namespace OutputTextSeletion
{
    /// <summary>
    /// Do select operation
    /// </summary>
    public class WlTextSelection
    {
        private DTE2 _app = null;
        private UIHierarchy _rootNode = null;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        public WlTextSelection(DTE2 app)
        {
            _app = app;
            _rootNode = _app.ToolWindows.SolutionExplorer;
        }

        /// <summary>
        /// Select the next output text of WriteLine
        /// </summary>
        /// <param name="selectType"></param>
        public void WlSelect(string selectType)
        {
            Debug.WriteLine(selectType);
            // (Console)(\\s*)(\\.)(\\s*)(WriteLine)(\\s*)(\\()(\\s*)(\")
            const string startPattenText = "(\\.)(\\s*)(WriteLine)(\\s*)(\\()(\\s*)(\")"; // (Console)(\s*)(\.)(\s*)(WriteLine)(\s*)(\()(\s*)(\")

            const string endPattenText = "(\")(\\s*)(\\))";

            if (_app.ActiveDocument == null) return;
            if (_app.ActiveDocument.Selection == null) return;
            var selected = _app.ActiveDocument.Selection as TextSelection;

            if (selected != null)
            {
                if (selectType == "fwd")
                {

                    int selectionStartAbsoluteOffset = 0;
                    int selectionEndAbsoluteOffset = 0;
                    int tmpSelectionStartAbsoluteOffset = 0;
                    int tmpSelectionEndAbsoluteOffset = 0;

                    // Save the current selection:
                    tmpSelectionStartAbsoluteOffset = selected.TopPoint.AbsoluteCharOffset;
                    tmpSelectionEndAbsoluteOffset = selected.BottomPoint.AbsoluteCharOffset;
                    if (selected.Text.Length > 0)
                    {
                        selected.MoveToAbsoluteOffset(tmpSelectionStartAbsoluteOffset, false);
                    }

                    if (selected.FindPattern(startPattenText, (int)vsFindOptions.vsFindOptionsRegularExpression))
                    {
                        selectionStartAbsoluteOffset = selected.BottomPoint.AbsoluteCharOffset;

                        if (selected.FindPattern(endPattenText, (int)vsFindOptions.vsFindOptionsRegularExpression))
                        {
                            selectionEndAbsoluteOffset = selected.TopPoint.AbsoluteCharOffset;

                            // Restore the original selection:
                            selected.MoveToAbsoluteOffset(selectionStartAbsoluteOffset, false);
                            selected.MoveToAbsoluteOffset(selectionEndAbsoluteOffset, true);
                        }
                        else
                        {
                            selected.MoveToAbsoluteOffset(tmpSelectionStartAbsoluteOffset, false);
                            selected.MoveToAbsoluteOffset(tmpSelectionEndAbsoluteOffset, true);
                        }
                    }
                    else
                    {
                        selected.MoveToAbsoluteOffset(tmpSelectionEndAbsoluteOffset, true);
                    }
                }
                else if (selectType == "bwd")
                {
                    int tmpSelectionStartAbsoluteOffset = 0;
                    int tmpSelectionEndAbsoluteOffset = 0;

                    // Save the current selection:
                    tmpSelectionStartAbsoluteOffset = selected.TopPoint.AbsoluteCharOffset;
                    tmpSelectionEndAbsoluteOffset = selected.BottomPoint.AbsoluteCharOffset;

                    int selectionStartAbsoluteOffset = 0;
                    int selectionEndAbsoluteOffset = 0;

                    //selectionStartAbsoluteOffset = selected.TopPoint.AbsoluteCharOffset;
                    //selectionEndAbsoluteOffset = selected.TopPoint.AbsoluteCharOffset;

                    selected.StartOfDocument();

                    while (selected.FindPattern(startPattenText, (int)vsFindOptions.vsFindOptionsRegularExpression))
                    {
                        if (selected.BottomPoint.AbsoluteCharOffset < tmpSelectionStartAbsoluteOffset ||
                            (selected.Text == "" &&
                                 selected.BottomPoint.AbsoluteCharOffset == tmpSelectionStartAbsoluteOffset))
                        {
                            selectionStartAbsoluteOffset = selected.BottomPoint.AbsoluteCharOffset;
                        }
                        else
                        {
                            if (selectionStartAbsoluteOffset != 0)
                            {
                                selected.MoveToAbsoluteOffset(selectionStartAbsoluteOffset, false);
                            }
                            break;
                        }
                    }

                    if (selectionStartAbsoluteOffset != 0 && selected.FindPattern(endPattenText, (int)vsFindOptions.vsFindOptionsRegularExpression))
                    {
                        selectionEndAbsoluteOffset = selected.TopPoint.AbsoluteCharOffset;
                        // Restore the original selection:
                        selected.MoveToAbsoluteOffset(selectionStartAbsoluteOffset, false);
                        selected.MoveToAbsoluteOffset(selectionEndAbsoluteOffset, true);
                    }
                    else
                    {
                        selected.MoveToAbsoluteOffset(tmpSelectionStartAbsoluteOffset, false);
                        selected.MoveToAbsoluteOffset(tmpSelectionEndAbsoluteOffset, true);
                    }
                }
            }
        }
    }
}
;