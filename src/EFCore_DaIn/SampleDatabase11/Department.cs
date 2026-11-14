using System.Collections.Generic;

namespace SampleDatabase11;

public partial class Department
{
    public Department()
    {
        Employee = new HashSet<Employee>();
        Name = string.Empty;
    }

    public int Id { get; set; }
    public string Name { get; set; }

    public virtual ICollection<Employee> Employee { get; set; }
}
