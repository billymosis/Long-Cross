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
            using (Transaction tr = doc.TransactionManager.StartTransaction())
            {
                TypedValue[] TV = new TypedValue[1];
                TV.SetValue(new TypedValue((int)DxfCode.Start, "TEXT,MTEXT"), 0);
                SelectionFilter SF = new SelectionFilter(TV);
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
                            if (c.TextString.Substring(0, 1) == "+" || c.TextString.Substring(0, 1) == "-" || c.TextString.Substring(0, 1) == "±")
                            {
                                string myValue = c.TextString.Replace(" ", "");
                                if (myValue.Substring(0, 1) == "±")
                                {
                                    myValue = myValue.Substring(1);
                                }
                                double Elev = double.Parse(myValue) + dob.Value;
                                c.TextString = (Elev > 0 ? "+" : "") + Math.Round(Elev, 2).ToString();
                            }
                        }
                        else
                        {
                            MText c = item.GetObject(OpenMode.ForWrite) as MText;
                            string cb = c.Text;
                            if (c.Text.Substring(0, 1) == "+" || c.Text.Substring(0, 1) == "-" || c.Text.Substring(0, 1) == "±")
                            {
                                string myValue = cb.Replace(" ", "");
                                if (myValue.Substring(0, 1) == "±")
                                {
                                    myValue = myValue.Substring(1);
                                }
                                double Elev = double.Parse(myValue) + dob.Value;
                                c.Contents = (Elev > 0 ? "+" : "") + Elev.ToString("{0.00}");
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
        }
    }
}