﻿using ProtoBuf;

namespace Modules.ProtoBuf.Test.Domain
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllFields)]
    internal class Employee
    {
      //  [ProtoMember(1)]
        public string Name { get; set; }
        //[ProtoMember(2)]
        public int Age { get; set; }

        public readonly int ShoeSize;

        public Employee()
        {
            
        }

        public Employee(int shoeSize)
        {
            ShoeSize = shoeSize;
        }
    }
}
