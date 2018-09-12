using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.ProjectOxford.Vision.Contract;

namespace LiveCameraSample
{
    public class AtosEmployeesResult
    {
        public AtosEmployee[] AtosEmployees { get; set; }
    }

    public class AtosEmployee
    {
        public string PersonId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string DasId { get; set; }
        public FaceRectangle FaceRectangle { get; set; }
        public float Confidence { get; set; }
        public string Email { get; set; }
    }
}
