﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OWolverine.Models.Ogame
{
    [XmlRoot("alliances")]
    public class AllianceList
    {
        [XmlElement("alliance")]
        public List<Alliance> Alliances { get; set; }
    }


    public class Alliance
    {
        [XmlAttribute("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }
        [XmlAttribute("tag")]
        public string Tag { get; set; }
        [XmlAttribute("founder")]
        public int FounderId { get; set; }
        [XmlIgnore]
        public Player Founder { get; set; }
        [XmlElement("player")]
        public List<Player> Members { get; set; }
        [XmlAttribute("open")]
        public bool IsOpen { get; set; }

        public int ServerId { get; set; }
        [ForeignKey("ServerId")]
        public Universe Server { get; set; }
    }

    public class AllianceConfiguration : IEntityTypeConfiguration<Alliance>
    {
        public void Configure(EntityTypeBuilder<Alliance> builder)
        {
            builder.ToTable("Alliance", "og");
            builder.HasKey(e => new { e.Id, e.ServerId });
            builder.HasOne(e => e.Founder)
                .WithOne()
                .HasForeignKey<Alliance>(e => new { e.FounderId, e.ServerId });
        }
    }
}
