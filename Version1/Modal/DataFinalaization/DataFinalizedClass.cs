using System.ComponentModel.DataAnnotations;

namespace SQCScanner.Modal.DataFinalaization
{
    public class DataFinalizedClass
    {
        [Key]
        public int Sr { get; set; }
        public DateTime Time { get; set; }
        public int EmpId { get; set; } = 0;
        public int TestId { get; set; } = 0;
        public string Remark { get; set; } = "";
        public string state { get; set; } = "";
       
    }
}
