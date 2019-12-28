using System;
using MySql.Data.MySqlClient;
using System.Data;
using Iodine;
using Iodine.Runtime;
using System.Dynamic;
using System.Collections.Generic;

namespace ModuleMySQL
{
    public class IodineMySQLParameter : IodineObject
    {
        private static readonly IodineTypeDefinition MySQLParamConnectionTypeDef = new IodineTypeDefinition ("MySQLParameter");

        public string ParameterName {
            get;
            private set;
        }

        public IodineObject Value {
            get;
            private set;
        }

        public IodineMySQLParameter (string paramName, IodineObject value) : base (MySQLParamConnectionTypeDef)
        {
            this.ParameterName = paramName;
            this.Value = value;
        }
    }

    public class IodineMySQLConnection : IodineObject
    {
        private static readonly IodineTypeDefinition MySQLConnectionTypeDef = new IodineTypeDefinition ("MySQLConnection");

        public MySqlConnection Connection {
            get;
            private set;
        }

        public IodineMySQLConnection (MySqlConnection con) : base (MySQLConnectionTypeDef)
        {
            this.Connection = con;
            SetAttribute ("executeSql", new BuiltinMethodCallback (executeSql, this));
            SetAttribute ("executeSqlPrepared", new BuiltinMethodCallback (executeSqlPrepared, this));
            SetAttribute ("querySql", new BuiltinMethodCallback (querySql, this));
            SetAttribute ("querySqlPrepared", new BuiltinMethodCallback (querySqlPrepared, this));
            SetAttribute ("close", new BuiltinMethodCallback (close, this));
            SetAttribute ("createParam", new BuiltinMethodCallback (createParam, this));
        }

        public IodineMySQLParameter createParam (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            return new IodineMySQLParameter (((IodineString)args [0]).Value, args [1]);
        }

        private MySqlCommand prepareCmd (ref MySqlCommand cmd, IodineObject[] args)
        {
            cmd.Prepare ();
            for (int i = 1; i < args.Length; i++) {
                IodineMySQLParameter arg = (IodineMySQLParameter)args [i];
                cmd.Parameters.Add (new MySqlParameter (arg.ParameterName, arg.Value.ToString ()));
            }
            return cmd;
        }

        public IodineObject executeSqlPrepared (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            var db = (IodineMySQLConnection)self;
            var statement = (IodineString)args [0];
            MySqlCommand cmd = new MySqlCommand (statement.Value, db.Connection);
            prepareCmd (ref cmd, args);
            cmd.ExecuteNonQuery ();
            cmd.Dispose ();
            return null;
        }

        public IodineObject executeSql (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            var db = (IodineMySQLConnection)self;
            var statement = (IodineString)args [0];
            MySqlCommand cmd = new MySqlCommand (statement.Value, db.Connection);
            cmd.ExecuteNonQuery ();
            cmd.Dispose ();
            return null;
        }

        private IodineList convertDataTable (DataTable table)
        {
            var resList = new IodineList (new IodineObject[]{ });

            foreach (DataRow row in table.Rows) {
                var items = new IodineDictionary ();
                foreach (DataColumn col in table.Columns) {
                    items.Set (new IodineString (col.ColumnName), new IodineString ((row [col.ColumnName]).ToString ()));
                }
                resList.Add (items);
            }
            return resList;
        }

        public IodineObject querySqlPrepared (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            var db = (IodineMySQLConnection)self;
            var statement = (IodineString)args [0];
            MySqlCommand cmd = new MySqlCommand (statement.Value, db.Connection);
            prepareCmd (ref cmd, args);
            var reader = cmd.ExecuteReader ();
            var table = new DataTable ();
            table.Load (reader);
            reader.Dispose ();
            cmd.Dispose ();
            return convertDataTable (table);
        }

        public IodineObject querySql (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            var db = (IodineMySQLConnection)self;
            var statement = (IodineString)args [0];
            MySqlCommand cmd = new MySqlCommand (statement.Value, db.Connection);
            var reader = cmd.ExecuteReader ();
            var table = new DataTable ();
            table.Load (reader);
            reader.Dispose ();
            cmd.Dispose ();
            return convertDataTable (table);
        }

        public IodineObject close (VirtualMachine vm, IodineObject self, IodineObject[] args)
        {
            var db = (IodineMySQLConnection)self;
            db.Connection.Close ();
            db.Connection.Dispose ();
            return null;
        }
    }
}
