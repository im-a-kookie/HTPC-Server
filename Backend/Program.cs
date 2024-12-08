﻿using CookieCrumbs;
using CookieCrumbs.ContentLibrary;
using CookieCrumbs.Serializing;
using CookieCrumbs.TCPMediation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.ServerLibrary
{
    public class Program
    {
        public static SerializationEngine localSerializer = new();
        public static SerializationEngine remoteSerializer = new();

        public static void Main(string[] args)
        {

            ConnectionProvider cp = new ConnectionProvider(6556);
            while(true)
            {
                Console.ReadLine();
            }


            // Register the serializer reconstructors for the local builder
            localSerializer.RegisterBuilder("File", () => new MediaFile());
            localSerializer.RegisterBuilder("Season", () => new Season());
            localSerializer.RegisterBuilder("Title", () => new Title("_undefined"));

            // The remote rebuilder is essentially the same
            // But lets us use different classes, provided they offer the same properties
            remoteSerializer.RegisterBuilder("File", () => new MediaFile());
            remoteSerializer.RegisterBuilder("Season", () => new Season());
            remoteSerializer.RegisterBuilder("Title", () => new Title("_undefined"));

            var searcher = new Searcher("E:/");
            var lib = searcher.Enumerate(4);





            foreach(var title in lib.FoundSeries)
            {
                var s = localSerializer.GetCompounded(title.Value);
                var d = localSerializer.CompoundToString(s);

                var _d = localSerializer.Rebuild(d);

                Console.WriteLine(d);
            }



            Console.WriteLine("Done!");
        }






    }


}