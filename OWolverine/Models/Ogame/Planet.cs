﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OWolverine.Models.Ogame
{
    [Serializable()]
    [XmlRoot(ElementName = "planet")]
    public class Planet
    {
        [XmlAttribute("id")]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int Id { get; set; }
        public int ServerId { get; set; }
        [XmlAttribute("name")]
        public string Name { get; set; }

        private Coordinate _coords { get; set; } = new Coordinate();
        [XmlAttribute("coords")]
        public string Coords {
            get {
                return String.Format("{0}:{1}:{2}", _coords.Galaxy, _coords.System, _coords.Location);
            }
            set {
                var arr = value.Split(":");
                _coords.Galaxy = Convert.ToInt32(arr[0]);
                _coords.System = Convert.ToInt32(arr[1]);
                _coords.Location = Convert.ToInt32(arr[2]);
            }
        }

        private string _playerId { get; set; }
        [XmlAttribute("player")]
        public string PlayerId {
            get {
                return _playerId;
            }
            set {
                Owner.ServerId = _playerId = value;
            }
        }
        public Player Owner { get; set; } = new Player();
    }

    public class Coordinate
    {
        public int Galaxy { get; set; }
        public int System { get; set; }
        public int Location { get; set; }
    }
}
