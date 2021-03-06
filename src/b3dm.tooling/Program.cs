﻿using System;
using System.IO;
using System.Text;
using B3dm.Tile;
using CommandLine;
using glTFLoader;

namespace b3dm.tooling
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<PackOptions, UnpackOptions, InfoOptions>(args).WithParsed(o =>
            {
                switch (o)
                {
                    case InfoOptions options:
                        Info(options);
                        break;
                    case PackOptions options:
                        Pack(options);
                        break;
                    case UnpackOptions options:
                        Unpack(options);
                        break;
                }
            });
        }

        static void Pack(PackOptions o)
        {
            Console.WriteLine($"Action: Pack");
            Console.WriteLine($"Input: {o.Input}");
            var f = File.ReadAllBytes(o.Input);
            var batchFile = Path.GetFileNameWithoutExtension(o.Input) + ".batch";
            var b3dm = new B3dm.Tile.B3dm(f);

            if (File.Exists(batchFile))
            {
                Console.WriteLine($"Input batch file: {batchFile}");
                var batchTableJson = File.ReadAllLines(batchFile);
                b3dm.FeatureTableJson = batchTableJson[0];
                b3dm.BatchTableJson = batchTableJson[1];
            }

            var b3dmfile = (o.Output == string.Empty ? Path.GetFileNameWithoutExtension(o.Input) + ".b3dm" : o.Output);

            if (File.Exists(b3dmfile) && !o.Force)
            {
                Console.WriteLine($"File {b3dmfile} already exists. Specify -f or --force to overwrite existing files.");
            }
            else
            {
                B3dmWriter.WriteB3dm(b3dmfile, b3dm);
                Console.WriteLine("B3dm created " + b3dmfile);
            }
        }

        static void Unpack(UnpackOptions o) { 
            Console.WriteLine($"Action: Unpack");
            Console.WriteLine($"Input: {o.Input}");
            var f = File.OpenRead(o.Input);
            var b3dm = B3dmReader.ReadB3dm(f);
            Console.WriteLine("b3dm version: " + b3dm.B3dmHeader.Version);
            var stream = new MemoryStream(b3dm.GlbData);
            try {
                var gltf = Interface.LoadModel(stream);
                Console.WriteLine("glTF asset generator: " + gltf.Asset.Generator);
                Console.WriteLine("glTF version: " + gltf.Asset.Version);
                var bufferBytes = gltf.Buffers[0].ByteLength;
                var glbfile = (o.Output==string.Empty?Path.GetFileNameWithoutExtension(o.Input) + ".glb": o.Output);
                var batchfile = (o.Output == string.Empty ? Path.GetFileNameWithoutExtension(o.Input) + ".batch" : o.Output);

                if (File.Exists(glbfile) && !o.Force)
                {
                    Console.WriteLine($"File {glbfile} already exists. Specify -f or --force to overwrite existing files.");
                }
                else
                {
                    File.WriteAllBytes(glbfile, b3dm.GlbData);
                    Console.WriteLine($"Glb created: {glbfile}");
                    if (b3dm.BatchTableJson != String.Empty)
                    {
                        var sb = new StringBuilder();
                        sb.Append(b3dm.FeatureTableJson);
                        sb.AppendLine();
                        sb.Append(b3dm.BatchTableJson);
                        File.WriteAllText(batchfile, sb.ToString());
                        Console.WriteLine($"batch file created: {batchfile}");
                    }
                }
            }
            catch (InvalidDataException ex) {
                Console.WriteLine("glTF version not supported.");
                Console.WriteLine(ex.Message);
            }
        }

        static void Info(InfoOptions o)
        {
            Console.WriteLine($"Action: Info");
            Console.WriteLine("b3dm file: " + o.Input);
            var f = File.OpenRead(o.Input);
            var b3dm = B3dmReader.ReadB3dm(f);
            Console.WriteLine("b3dm header version: " + b3dm.B3dmHeader.Version);
            Console.WriteLine("b3dm header magic: " + b3dm.B3dmHeader.Magic);
            Console.WriteLine("b3dm header bytelength: " + b3dm.B3dmHeader.ByteLength);
            Console.WriteLine("b3dm header featuretablejson length: " + b3dm.B3dmHeader.FeatureTableJsonByteLength);
            Console.WriteLine("b3dm header batchtablejson length: " + b3dm.B3dmHeader.BatchTableJsonByteLength);
            Console.WriteLine("Batch table json: " + b3dm.BatchTableJson);
            Console.WriteLine("Feature table json: " + b3dm.FeatureTableJson);
            var stream = new MemoryStream(b3dm.GlbData);
            try
            {
                var gltf = Interface.LoadModel(stream);
                Console.WriteLine("glTF model is loaded");
                Console.WriteLine("glTF generator: " + gltf.Asset.Generator);
                Console.WriteLine("glTF version:" + gltf.Asset.Version);
                Console.WriteLine("glTF primitives: " + gltf.Meshes[0].Primitives.Length);
                if (gltf.ExtensionsUsed != null)
                {
                    Console.WriteLine("glTF extensions used:" + string.Join(',', gltf.ExtensionsUsed));
                }
                else
                {
                    Console.WriteLine("glTF: no extensions used.");
                }
                if (gltf.ExtensionsRequired != null)
                {
                    Console.WriteLine("glTF extensions required:" + string.Join(',', gltf.ExtensionsRequired));
                }
                else
                {
                    Console.WriteLine("glTF: no extensions required.");
                }

                if (gltf.Meshes[0].Primitives.Length > 0)
                {
                    Console.WriteLine("glTF primitive mode: " + gltf.Meshes[0].Primitives[0].Mode);
                    Console.WriteLine("glTF primitive attributes: " + String.Join(',', gltf.Meshes[0].Primitives[0].Attributes));
                    // gltf.Meshes[0].Primitives[0];
                    // todo: how to get to the vertices?
                }
            }
            catch (InvalidDataException ex)
            {
                Console.WriteLine("glTF version not supported.");
                Console.WriteLine(ex.Message);
            }
            f.Dispose();
        }
    }
}
