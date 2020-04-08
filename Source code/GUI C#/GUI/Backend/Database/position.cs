namespace GUI.Backend.Database
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;
    using System.Data.Entity.Spatial;

    [Table("device_detection_db.position")]
    public partial class position
    {
        [Key]
        [Column(Order = 0)]
        public DateTime timestamp { get; set; }

        [Key]
        [Column(Order = 1)]
        [StringLength(20)]
        public string MACaddress { get; set; }

        [Key]
        [Column(Order = 2)]
        [StringLength(45)]
        public string configuration_id { get; set; }

        public double x { get; set; }

        public double y { get; set; }

        public int? sequence_number { get; set; }

        [StringLength(100)]
        public string SSID { get; set; }

        [Column(TypeName = "uint")]
        public long? fingerprint { get; set; }
    }
}
