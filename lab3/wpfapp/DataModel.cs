using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System;
using System.Collections.Generic;

namespace wpfapp
{
    public class ImageContext: DbContext 
    {
        public DbSet<ImageDB> Images { get; set; }
        public ImageContext() => Database.EnsureCreated();

        //public string DbPath { get; }
        
        // public ImageContext()
        // {
        //     var folder = Environment.SpecialFolder.LocalApplicationData;
        //     var path = Environment.GetFolderPath(folder);
        //     DbPath = System.IO.Path.Join(path, "babin_arcface.db");
        // }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite($"Data Source=babin_arcface.db");
    }

    public class ImageDB
    {
        [Key]
        public int id { get; set; }
        public string hash { get; set; }
        public string path { get; set; }
        public byte[] image { get; set; }
        public string embedding { get; set; }
    }
}