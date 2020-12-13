using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Runtime;
using System;

namespace PLC
{
    public class ChangeElevation
    {
        [CommandMethod("CE")]
        public void CE()
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;
            PromptResult result = ed.GetKeywords(new PromptKeywordOptions("choose your character [Select / All] :", "Select All"));
            switch (result.StringResult)
            {
                case "All":
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        BlockTable bt = tr.GetObject(db.BlockTableId, OpenMode.ForWrite) as BlockTable;
                        BlockTableRecord btr = tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;
                        PromptDoubleResult dob = ed.GetDouble("\nElevasi:");
                        if (dob.Status == PromptStatus.Cancel)
                        {
                            return;
                        }
                        foreach (ObjectId item in btr)
                        {
                            try
                            {
                                if (item.ObjectClass.DxfName == "TEXT")
                                {
                                    DBText c = item.GetObject(OpenMode.ForWrite) as DBText;
                                    if (c.TextString.Substring(0, 1) == "+")
                                    {
                                        double Elev = double.Parse(c.TextString.Substring(1)) + dob.Value;
                                        c.TextString = "+" + Math.Round(Elev, 2).ToString();
                                    }
                                }
                                if (item.ObjectClass.DxfName == "MTEXT")
                                {
                                    MText c = item.GetObject(OpenMode.ForWrite) as MText;
                                    string cb = c.Text;
                                    if (c.Text.Substring(0, 1) == "+")
                                    {
                                        double Elev = double.Parse(c.Text.Substring(1)) + dob.Value;
                                        c.Contents = c.Contents.Replace(cb, "+" + Math.Round(Elev, 2).ToString());
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                ed.WriteMessage(ex.Message);
                            }
                        }
                        tr.Commit();
                    }

                    ed.WriteMessage("\nEnd of Transaction");
                    break;

                case "Select":
                    using (Transaction tr = doc.TransactionManager.StartTransaction())
                    {
                        TypedValue[] TV = new TypedValue[1];
                        TV.SetValue(new TypedValue((int)DxfCode.Start, "TEXT,MTEXT"), 0);
                        SelectionFilter SF = new SelectionFilter(TV);
                        PromptSelectionOptions pso = new PromptSelectionOptions();
                        PromptSelectionResult selectionResult = ed.GetSelection(SF);
                        PromptDoubleResult dob = ed.GetDouble("\nElevasi:");
                        if (dob.Status == PromptStatus.Cancel)
                        {
                            return;
                        }
                        foreach (ObjectId item in selectionResult.Value.GetObjectIds())
                        {
                            try
                            {
                                if (item.ObjectClass.DxfName == "TEXT")
                                {
                                    DBText c = item.GetObject(OpenMode.ForWrite) as DBText;
                                    if (c.TextString.Substring(0, 1) == "+")
                                    {
                                        double Elev = double.Parse(c.TextString.Substring(1)) + dob.Value;
                                        c.TextString = "+" + Math.Round(Elev, 2).ToString();
                                    }
                                }
                                else
                                {
                                    MText c = item.GetObject(OpenMode.ForWrite) as MText;
                                    string cb = c.Text;
                                    if (c.Text.Substring(0, 1) == "+")
                                    {
                                        double Elev = double.Parse(c.Text.Substring(1)) + dob.Value;
                                        c.Contents = c.Contents.Replace(cb, "+" + Math.Round(Elev, 2).ToString());
                                    }
                                }
                            }
                            catch (System.Exception ex)
                            {
                                ed.WriteMessage(ex.Message);
                            }
                        }

                        tr.Commit();
                    }
                    break;
            }
        }
    }
}