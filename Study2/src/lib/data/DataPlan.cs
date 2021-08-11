﻿using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using static PLC.Utilities;

namespace PLC
{
    public class DataPlan
    {
        public Point3dCollection PatokPoint = new Point3dCollection();
        public Point3dCollection PointTanggulKiri = new Point3dCollection();
        public Point3dCollection PointTanggulKanan = new Point3dCollection();
        public Point3dCollection PointDasarKiri = new Point3dCollection();
        public Point3dCollection PointDasarKanan = new Point3dCollection();
        public Point3dCollection PointAlignment = new Point3dCollection();

        public List<string> namaPatok = new List<string>();
        public List<List<string>> descriptionList = new List<List<string>>();

        public List<Point3dCollection> RAWSurfaceData = new List<Point3dCollection>();
        public List<Point3dCollection> SurfaceData = new List<Point3dCollection>();
        public List<Point2dCollection> CutLineData = new List<Point2dCollection>();

        private readonly Data PlanData;

        public DataPlan(Data myData)
        {
            PlanData = myData;
            GetData(myData.CrossDataCollection);
        }

        public void GetData(List<DataCross> myData)
        {
            for (int i = 0; i < myData.Count; i++)
            {
                DataCross Patok = myData[i];
                DataCross PatokNext = i + 1 == myData.Count ? Patok : myData[i + 1];
                DataCross PatokBefore = i == 0 ? Patok : myData[i - 1];

                double AngleNext = 0;
                double AngleBefore = 0;

                namaPatok.Add(Patok.NamaPatok);
                if (Patok.Elevation.Contains(string.Empty))
                {
                    Debugger.Break();
                }
                List<double> Elev = Patok.Elevation.ConvertAll(x => double.Parse(x));
                List<double> Dist = Patok.Distance.ConvertAll(x => double.Parse(x));
                List<string> Desc = Patok.Description;

                Point3d KoordinatPatok = new Point3d(Patok.KX, Patok.KY, Patok.KZ);
                Point3d KoordinatPatokNext = new Point3d(PatokNext.KX, PatokNext.KY, PatokNext.KZ);
                Point3d KoordinatPatokBefore = new Point3d(PatokBefore.KX, PatokBefore.KY, PatokBefore.KZ);

                Point3dCollection Cross = new Point3dCollection();


                double SlopeNext = GetSlope(ConvertPoint2d(KoordinatPatokNext), ConvertPoint2d(KoordinatPatok));
                double SlopeBefore = GetSlope(ConvertPoint2d(KoordinatPatok), ConvertPoint2d(KoordinatPatokBefore));

                double ReciprocalNext = GetReciprocal(SlopeNext);
                double ReciprocalBefore = GetReciprocal(SlopeBefore);
                double Reciprocal = (ReciprocalNext + ReciprocalBefore);

                AngleNext = Math.Atan(ReciprocalNext);
                AngleBefore = Math.Atan(ReciprocalBefore);

                if (double.IsNaN(AngleNext))
                {
                    AngleNext = AngleBefore;
                }
                if (double.IsNaN(AngleBefore))
                {
                    AngleBefore = AngleNext;
                }

                double Angle = (AngleNext + AngleBefore) / 2;

                string SQuadrant = string.Concat(BothQuadrant(KoordinatPatokBefore, KoordinatPatok, KoordinatPatokNext));

                switch (SQuadrant)
                {
                    case ("12"):
                        Angle = Angle + (3 * Math.PI / 2); // 270 degree
                        break;
                    case ("21"):
                        Angle = Angle + (1 * Math.PI / 2); // 90 degree
                        break;
                    case ("31"):
                        Angle = Angle + (1 * Math.PI); // 180 degree
                        break;
                    case ("34"):
                        Angle = Angle + (1 * Math.PI / 2); // 90 degree
                        break;
                    case ("42"):
                        Angle = Angle + (1 * Math.PI); // 180 degree
                        break;
                    case ("43"):
                        Angle = Angle + (3 * Math.PI / 2); // 270 degree
                        break;
                }

                Point3d AlignmentPoint = PolarPoints(KoordinatPatok, Angle, Patok.Intersect.X);
                AlignmentPoint = new Point3d(AlignmentPoint.X, AlignmentPoint.Y, Patok.Intersect.Y);

                if (double.IsNaN(AlignmentPoint.X))
                {
                    Debugger.Break();
                }

                PatokPoint.Add(KoordinatPatok);


                if (Patok.PatokSaja)
                {
                    if (i == 0)
                    {
                        PointAlignment.Add(AlignmentPoint);
                    }
                    else
                    {

                        if (string.IsNullOrEmpty(Patok.Bangunan))
                        {

                            PointAlignment.Add(PointAlignment[PointAlignment.Count - 1]);
                        }
                    }
                }
                else
                {
                    PointAlignment.Add(AlignmentPoint);
                }



                Point3dCollection RAWSurface = new Point3dCollection();
                List<string> crossDescriptionData = new List<string>();

                for (int j = 0; j < Dist.Count; j++)
                {
                    Point3d PointKoordinat = PolarPoints(KoordinatPatok, Angle, Dist[j]);
                    PointKoordinat = new Point3d(PointKoordinat.X, PointKoordinat.Y, Elev[j]);
                    crossDescriptionData.Add(Desc[j]);

                    Cross.Add(PointKoordinat);
                    RAWSurface.Add(PointKoordinat);

                    if (Patok.PatokSaja)
                    {
                        if (i == 0)
                        {
                            if (j == Patok.TanggulKiriIndex)
                            {
                                PointTanggulKiri.Add(PointKoordinat);
                            }
                            if (j == Patok.TanggulKananIndex)
                            {
                                PointTanggulKanan.Add(PointKoordinat);
                            }
                            if (j == Patok.DasarKiriIndex)
                            {
                                PointDasarKiri.Add(PointKoordinat);
                            }
                            if (j == Patok.DasarKananIndex)
                            {
                                PointDasarKanan.Add(PointKoordinat);
                            }


                        }
                        else
                        {
                            if (string.IsNullOrEmpty(Patok.Bangunan))
                            {
                                PointTanggulKiri.Add(PointTanggulKiri[PointTanggulKiri.Count - 1]);
                                PointTanggulKanan.Add(PointTanggulKanan[PointTanggulKanan.Count - 1]);
                                PointDasarKiri.Add(PointDasarKiri[PointDasarKiri.Count - 1]);
                                PointDasarKanan.Add(PointDasarKanan[PointDasarKanan.Count - 1]);
                            }
                        }

                    }
                    else
                    {
                        if (j == Patok.TanggulKiriIndex)
                        {
                            PointTanggulKiri.Add(PointKoordinat);
                        }
                        if (j == Patok.TanggulKananIndex)
                        {
                            PointTanggulKanan.Add(PointKoordinat);
                        }
                        if (j == Patok.DasarKiriIndex)
                        {
                            PointDasarKiri.Add(PointKoordinat);
                        }
                        if (j == Patok.DasarKananIndex)
                        {
                            PointDasarKanan.Add(PointKoordinat);
                        }
                    }


                }
                descriptionList.Add(crossDescriptionData);
                RAWSurfaceData.Add(RAWSurface);

                //if (this.PointAlignment.Count != this.PointTanggulKiri.Count)
                //{
                //    Debugger.Break();
                //}

                Point2d coordinateTanggulKiri2D = ConvertPoint2d(Cross[Patok.TanggulKiriIndex]);
                Point2d coordinateTanggulKanan2D = ConvertPoint2d(Cross[Patok.TanggulKananIndex]);
                Point2d midPoint = GetMidPoint(coordinateTanggulKiri2D, coordinateTanggulKanan2D);

                Point2dCollection PointCut = new Point2dCollection
                {
                    coordinateTanggulKiri2D,
                    midPoint,
                    coordinateTanggulKanan2D
                };
                CutLineData.Add(PointCut);


                List<Point3d> CrossPoint = ConvertP3DC2List(Cross);

                CrossPoint.RemoveRange(0, Patok.TanggulKiriIndex);
                int UpdatePatokKanan = Patok.TanggulKananIndex - Patok.TanggulKiriIndex + 1;
                if (UpdatePatokKanan < 0)
                {
                    UpdatePatokKanan = 0;
                }
                CrossPoint.RemoveRange(UpdatePatokKanan, CrossPoint.Count - UpdatePatokKanan);

                Cross.Clear();
                foreach (Point3d item in CrossPoint)
                {
                    Cross.Add(item);
                }

                SurfaceData.Add(Cross);
            }

        }

        /// <summary>
        /// Untuk Check Error yang terjadi ketika pembetukan poligon cross.
        /// nanti hubungannya akah ke derajat antara patok dan azimuth untuk next feature.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="NamaPatok"></param>
        /// <param name="AngleBefore"></param>
        /// <param name="AngleNext"></param>
        /// <param name="Angle"></param>
        /// <param name="SlopeBefore"></param>
        /// <param name="SlopeNext"></param>
        /// <param name="SQuadrant"></param>
        private static void CheckErrorDerajat(int index, string NamaPatok, double AngleBefore, double AngleNext, double Angle, double SlopeBefore, double SlopeNext, double SQuadrant)
        {
            Document Doc = Application.DocumentManager.MdiActiveDocument;
            Editor ed = Doc.Editor;

            if (index > 620)
            {
                ed.WriteMessage(NamaPatok + " : " + " Angle " + CR2D(AngleBefore).ToString() + " " + CR2D(AngleNext).ToString() + " " + CR2D(Angle).ToString() + "\n");
                ed.WriteMessage(NamaPatok + " : " + " Slope " + SlopeBefore.ToString() + " " + SlopeNext.ToString() + "\n");
                ed.WriteMessage(NamaPatok + " : " + " Quadrant " + SQuadrant + "\n");
            }
        }


    }
}