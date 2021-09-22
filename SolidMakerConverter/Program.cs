/*
  SolidMakerConverter - A tool to be used for parsing sliced files from
  Cura 4.9.1 for the discontinued Solidmaker SLA 3D Printer
  
  Copyright (c) 2020-2021 Daniel Olsson
  
  SolidMakerConverter is free software: you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation, either version 3 of the License, or
  (at your option) any later version.
  SolidMakerConverter is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.
  You should have received a copy of the GNU General Public License
  along with SolidMakerConverter. If not, see <http://www.gnu.org/licenses/>.
*/
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SolidMakerConverter
{
    class Program
    {
        static bool firstFound = false;
        static double LiftHeight = 8.0;
        static MoveType activeMoveType = new MoveType("FirstLayer", "any first layer move", "", "T3");
        static int G0Speed = 72000;
        static int ZSpeed = 300;
        static int layercount = -1;
        static int linecount = 0;

        static List<string> ignorelist = new List<string> {
            ";TIME:",
            ";Filament used:",
            ";MINX:",
            ";MINY:",
            ";MINZ:",
            ";MAXX",
            ";MAXY",
            ";MAXZ",
            "G92 E0",
            "M107",
            ";MESH:",
            ";TIME_ELAPSED:"};

        static List<MoveType> typeList = new List<MoveType> {
            new MoveType("FirstLayer", "any first layer move", ";TYPE: FIRST LAYER", "T3"),
            new MoveType("Outter Wall", "the outmost line", ";TYPE:WALL-OUTER", "T2"),
            new MoveType("Inner Wall", "Inside the outter wall, facing eachother and the fill ", ";TYPE:WALL-INNER", "T1"),
            new MoveType("Infill", "inner fill of the model", ";TYPE:FILL", "T1"),
            new MoveType("Skin", "The top outter wall surface", ";TYPE:SKIN", "T2") };

        static void Main(string[] args)
        {
            DateTime startTime = DateTime.Now;
            string outfile = args[0] + ".slm";
            Console.WriteLine("== GCode conversion for Solidmaker ==");
            Console.WriteLine(" - InputFile: " + args[0]);
            Console.WriteLine(" - OutputFile: " + outfile);
            Console.Write("Ignoring the following GCode Lines:");
            foreach (string igit in ignorelist)
                Console.WriteLine("    \"" + igit + ", ");

            string TotalLayers = "";

            StreamReader sr1 = File.OpenText(args[0]);
            string? s1 = sr1.ReadLine();
            bool found1 = false;
            while (s1 != null && !found1)
            {
                if (s1.Contains(";LAYER_COUNT:")) //actual line is:";LAYER:0" for first layer
                {
                    Match resultCount = Regex.Match(s1, ";LAYER_COUNT:(\\d+)");

                    if (resultCount == Match.Empty)
                        throw new Exception("Unexpected missing layercount number");

                    TotalLayers = "M718 L";
                    if (Match.Empty != resultCount)
                        TotalLayers += "" + resultCount.Groups[1];
                    found1 = true;
                }
                s1 = sr1.ReadLine();
            }

            StreamReader sr = File.OpenText(args[0]);
            string? s = sr.ReadLine();
            int linenumber = 0;

            activeMoveType = typeList.First();

            bool firstLayer = true;

            int tool1Speed = 300;
            int tool2Speed = 600;
            int tool3Speed = 30;
            int tool1LaserIntensity = 18;
            int tool2LaserIntensity = 22;
            int tool3LaserIntensity = 30;

            var sw = File.Create(outfile);

            sw.Write(Encoding.ASCII.GetBytes(TotalLayers + "\n" +
                "M701 S0\n" +
                "M712 N50\n" +
                "G28 Z0\n" +
                "M910 W" + tool1LaserIntensity + " B" + tool1Speed + " E1200 F4095 T1500\n" +
                "M920 W" + tool2LaserIntensity + " B" + tool2Speed + " E1200 F4095 T1500\n" +
                "M930 W" + tool3LaserIntensity + " B" + tool3Speed + " E1200 F4095 T1500\n"));

            string fileimage = "";

            Console.WriteLine("Processing - (one '.' per layer)");

            while (s != null)
            {
                linecount++;
                if (s.StartsWith("G0"))
                    fixG0CodeLine(sw, s);

                else if (s.StartsWith("G1"))
                    fixG1CodeLine(sw, s, activeMoveType.Tool);

                else
                {
                    bool ignore = false;
                    foreach (string igstr in ignorelist)
                        if (s.StartsWith(igstr))
                            ignore = true;

                    if (!ignore)
                    {
                        if (s.StartsWith('M'))
                        {
                            fixMCodeLine(sw, s);
                        }
                        else if (s.StartsWith(';'))
                        {
                            if (s.Contains(";TYPE:"))
                            {
                                if (layercount > 0)
                                {
                                    int found = 0;
                                    foreach (MoveType mt in typeList)
                                    {
                                        if (s.StartsWith(mt.Marker))
                                        {
                                            //Set tool accordingly
                                            activeMoveType = mt;
                                            found++;
                                        }
                                    }
                                    if (found == 1)
                                        sw.Write(Encoding.ASCII.GetBytes(s + "\n"));
                                    else
                                        throw new Exception("Multiple movetypes matched... Expected Unique match.");
                                }
                                else
                                {
                                    //Since we are on the first layer - just insert the line for info
                                    sw.Write(Encoding.ASCII.GetBytes(";Type IGNORED on first layer: " + s + "\n"));
                                }
                            }
                            else if (s.Contains(";Generated with Cura_SteamEngine"))
                            {
                            }
                            else if (s.Contains(";LAYER:")) //actual line is:";LAYER:0" for first layer
                            {
                                layercount++;
                                sw.Write(Encoding.ASCII.GetBytes(s + "\n")); // Add the marker line
                                sw.Write(Encoding.ASCII.GetBytes("M728 L" + layercount + "\n"));
                                
                                Console.Write('.');
                            }
                            else if (s.Contains(";LAYER_COUNT:"))
                            {
                            }
                            else if (s.Contains(";Layer height:"))
                            {
                                sw.Write(Encoding.ASCII.GetBytes(s + "\n")); // Add the marker line
                            }
                            else if (s.Contains(";IMGDATA:"))
                            {
                                string[] a = s.Split(";IMGDATA:");
                                fileimage += a[1];
                            }
                            else //unknown ;Line
                                sw.Write(Encoding.ASCII.GetBytes(s + "\n"));
                        }
                        else if (s.StartsWith("G28"))
                        {
                        }
                        //else
                        //    throw new Exception("Not Supported - Line not beginning with 'G0', 'G1', 'G28', 'M' or ';'");
                    }
                }
                s = sr.ReadLine();
                linenumber++;
            }
            sw.Flush();
            Console.WriteLine(' ');

            Console.WriteLine("GCode processing complete\n\nProcess Base64 bmp thumbnail from Cura to binary data...");

            byte[] newBytes = Convert.FromBase64String(fileimage);
            sw.Write(Encoding.ASCII.GetBytes("M714 T1.1 T1.1 T1.1 T1.1\n M715" + "\n;"));
            sw.Write(newBytes);
            sw.Write(Encoding.ASCII.GetBytes("\nM716 L" + newBytes.Length + "\n"));
            sw.Write(newBytes);
            sw.Write(Encoding.ASCII.GetBytes("\nM717 L" + newBytes.Length + "\n"));
            sw.Flush();
            sw.Close();

            TimeSpan elapsed = DateTime.Now - startTime;
            Console.WriteLine("Base64 decoding complete!");
            Console.WriteLine("\nStats:\nTotal rows: " + linenumber + "\n"
                + "Total layers: " + layercount + "\n"
                + "Time elapsed: " + elapsed.ToString() + "\n"
                + "SolidmakerConverter finished!");
        }

        static string xpattern = "[X](-?\\d+(\\.\\d+)?)";
        static string ypattern = "[Y](-?\\d+(\\.\\d+)?)";
        static string zpattern = "[Z](-?\\d+(\\.\\d+)?)";

        static void fixG0CodeLine(FileStream sw, string s)
        {
            Match resultx = Regex.Match(s, xpattern);
            Match resulty = Regex.Match(s, ypattern);
            Match resultz = Regex.Match(s, zpattern);
            
            string xyLine = "G0 F" + G0Speed;
            if (Match.Empty != resultx)
                xyLine += " " + resultx.Groups[0];
            if (Match.Empty != resulty)
                xyLine += " " + resulty.Groups[0];
            if (Match.Empty != resultx || Match.Empty != resulty)
                sw.Write(Encoding.ASCII.GetBytes(xyLine + "\n"));

            string zLine1 = "G0 F" + ZSpeed;
            string zLine2 = "G0 F" + ZSpeed;
            if (Match.Empty != resultz)
            {
                double zval = double.Parse(resultz.Groups[1].Value.Replace('.' , ','));
                //Move Up
                zLine1 += " Z" + (zval + LiftHeight);
                sw.Write(Encoding.ASCII.GetBytes(zLine1.Replace(',' , '.') + "\n"));
                //Move Down
                zLine2 += " Z" + (zval + 0.0);
                sw.Write(Encoding.ASCII.GetBytes(zLine2.Replace(',', '.') + "\n"));
            }
        }

        static void fixG1CodeLine(FileStream sw, string s, string tool)
        {
            Match resultx = Regex.Match(s, xpattern);
            Match resulty = Regex.Match(s, ypattern);
            Match resultz = Regex.Match(s, zpattern);

            if (resultz != Match.Empty)
                throw new Exception("Unexpected G1 Z move - illegal ( Z moves are supposed to be G0 ) ");

            string xyLine = "G1";
            if (Match.Empty != resultx)
                xyLine += " " + resultx.Groups[0];
            if (Match.Empty != resulty)
                xyLine += " " + resulty.Groups[0];
            if(Match.Empty != resultx || Match.Empty != resulty)
                sw.Write(Encoding.ASCII.GetBytes(xyLine + " " + tool + "\n"));

        }

        static void fixMCodeLine(FileStream sw, string s)
        {
            return;
        }
    }
    class MoveType
    {
        public MoveType(string name, string description, string marker, string tool)
        {
            Name = name;
            Desctiption = description;
            Marker = marker;
            Tool = tool;
        }
        public String Name;
        public String Desctiption;
        public String Marker;
        public String Tool;

    }
}
