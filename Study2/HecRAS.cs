using Autodesk.AutoCAD.Geometry;
using System.Collections.Generic;

public class HecRAS
{
    public string GEORAS { get; set; }
    public int NumberofReaches;
    public int NumberofXS;
    public int NumberofCrossSection;
    public string UNITS;
    public string RiverName;
    public string ReachID;
    public Point3dCollection Alignment;
    public double Station;
    public double LOBDistance;
    public double ROBDistance;
    public List<double> LOBNext = new List<double>();
    public List<double> ChannelNext = new List<double>();
    public List<double> ROBNext = new List<double>();
    public List<Point2dCollection> CutLineData = new List<Point2dCollection>();
    public List<Point3dCollection> SurfaceData = new List<Point3dCollection>();
    public List<string> XSDistance = new List<string>();

    public HecRAS(Plan x)
    {
        RiverName = "OKERIVER";
        ReachID = "SungaiMantab";
        UNITS = "Feet";
        NumberofReaches = 1;
        Alignment = x.Alignment;
        NumberofXS = Alignment.Count;
        string HEADER = Header(RiverName, NumberofReaches, NumberofXS, UNITS);
        string Network = StreamNetwork(RiverName, ReachID, Alignment);
        string s = string.Empty;
        for (int i = 0; i < Alignment.Count; i++)
        {
            double JarakLOB;
            double JarakChannel;
            double JarakROB;
            if (i == 0)
            {
                JarakLOB = 0;
                JarakChannel = 0;
                JarakROB = 0;
                s += CrossSection(RiverName, ReachID, "0", JarakLOB, JarakChannel, JarakROB, x.CutLineData[i], x.SurfaceData[i]);
            }
            else
            {
                JarakLOB = x.TanggulKiri[i - 1].DistanceTo(x.TanggulKiri[i]);
                JarakChannel = Alignment[i - 1].DistanceTo(Alignment[i]);
                JarakROB = x.TanggulKanan[i - 1].DistanceTo(x.TanggulKanan[i]);
                s += CrossSection(RiverName, ReachID, XSDistance[i-1], JarakLOB, JarakChannel, JarakROB, x.CutLineData[i], x.SurfaceData[i]);
            }

        }
        GEORAS = HEADER + Network + s + "\rEND CROSS-SECTIONS:\r\r\r";
    }

    public string Header(string RiverName, int NumberofReaches, int NumberofXS, string UNITS)
    {
        string s = $"{RiverName}" +
            $"\r" +
            $"\r" +
            $"BEGIN HEADER:" +
            $"\r" +
            $"\r" +
            $"NUMBER OF REACHES: {NumberofReaches}" +
            $"\r" +
            $"NUMBER OF CROSS-SECTIONS: {NumberofXS}" +
            $"\r" +
            $"UNITS: {UNITS}" +
            $"\r" +
            $"\r" +
            $"END HEADER:\r";
        return s;
    }

    public string StreamNetwork(string RiverName, string ReachID, Point3dCollection Alignment)
    {
        string s = $"\rBEGIN STREAM NETWORK:\r";
        List<string> EndPoints = new List<string>();
        for (int i = 0; i < Alignment.Count; i++)
        {
            EndPoints.Add($"Endpoint: {Alignment[i].X.ToString("F")}, {Alignment[i].Y.ToString("F")}, {Alignment[i].Z.ToString("F")}, {(i + 1).ToString()}\r");
            s += EndPoints[i];
        }
        s += Reach(RiverName, ReachID, Alignment);
        s += CenterLine(Alignment);
        s += "\rEND STREAM NETWORK:\r\r" +
            "BEGIN CROSS-SECTIONS:\r\r\r";

        return s;
    }

    public string Reach(string RiverName, string ReachID, Point3dCollection Alignment)
    {
        string s = $"\rREACH:\r" +
            $"\r" +
            $"STREAM ID: {RiverName}" +
            $"\r" +
            $"REACH ID: {ReachID}" +
            $"\r" +
            $"FROM POINT: 1" +
            $"\r" +
            $"TO POINT: {Alignment.Count}" +
            $"\r";
        return s;
    }

    public string CenterLine(Point3dCollection Alignment)
    {
        string s = $"\rCENTERLINE:\r";
        double Distance = 0.00;
        List<string> EndPoints = new List<string>();
        for (int i = 0; i < Alignment.Count; i++)
        {
            EndPoints.Add($"{Alignment[i].X.ToString("F")}, {Alignment[i].Y.ToString("F")}, {Alignment[i].Z.ToString("F")}, {Distance.ToString("F")}\r");
            Distance += Alignment[i].DistanceTo(Alignment[i + 1]);
            XSDistance.Add(Distance.ToString("F"));
            s += EndPoints[i];
        }
        s += "END:\r";
        return s;
    }

    public string CrossSection(string RiverName, string ReachID, string Station, double LOBNext, double ChannelNext, double ROBNext, Point2dCollection CutLineData, Point3dCollection SurfaceData)
    {
        string s = $"CROSS-SECTION:\r\r" +
            $"STREAM ID: {RiverName}" +
            $"\r" +
            $"REACH ID: {ReachID}" +
            $"\r" +
            $"STATION: {Station}" +
            $"\r" +
            $"REACH LENGTHS: {LOBNext.ToString("F")},{ChannelNext.ToString("F")},{ROBNext.ToString("F")}" +
            $"\r";
        s += CutLine(CutLineData);
        s += SurfaceLine(SurfaceData);
        s += "\rEND:\r\r\r";
        return s;
    }

    public string CutLine(Point2dCollection CrossLine)
    {
        string s = $"\rCUT LINE:\r";
        List<string> EndPoints = new List<string>();
        for (int i = 0; i < CrossLine.Count; i++)
        {
            EndPoints.Add($"{CrossLine[i].X.ToString("F")}, {CrossLine[i].Y.ToString("F")}\r");
            s += EndPoints[i];
        }
        s += "\r";
        return s;
    }

    public string SurfaceLine(Point3dCollection SurfaceCrossData)
    {
        string s = $"SURFACE LINE:\r";
        List<string> EndPoints = new List<string>();
        for (int i = 0; i < SurfaceCrossData.Count; i++)
        {
            EndPoints.Add($"{SurfaceCrossData[i].X.ToString("F")}, {SurfaceCrossData[i].Y.ToString("F")}, {SurfaceCrossData[i].Z.ToString("F")}\r");
            s += EndPoints[i];
        }

        return s;
    }
}

