using System;
using System.Collections.Generic;
using System.Dynamic;
using Iodine.Runtime;
using MySql.Data.MySqlClient;

namespace ModuleMySQL
{

    [IodineBuiltinModule ("mysql")]
    public class MySQLModule : IodineModule
    {
        public MySQLModule ()
            : base ("mysql")
        {
            SetAttribute ("openDatabase", new BuiltinMethodCallback (openDatabase, this));
        }

        private IodineObject openDatabase (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            var db = new MySqlConnection (args [0].ToString ());
            db.Open ();
            return new IodineMySQLConnection (db);
        }

    }
}