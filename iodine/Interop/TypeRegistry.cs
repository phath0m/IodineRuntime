/**
  * Copyright (c) 2015, phath0m All rights reserved.

  * Redistribution and use in source and binary forms, with or without modification,
  * are permitted provided that the following conditions are met:
  * 
  *  * Redistributions of source code must retain the above copyright notice, this list
  *    of conditions and the following disclaimer.
  * 
  *  * Redistributions in binary form must reproduce the above copyright notice, this
  *    list of conditions and the following disclaimer in the documentation and/or
  *    other materials provided with the distribution.

  * Neither the name of the copyright holder nor the names of its contributors may be
  * used to endorse or promote products derived from this software without specific
  * prior written permission.
  * 
  * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
  * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
  * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT
  * SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT,
  * INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED
  * TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR
  * BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
  * CONTRACT ,STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN
  * ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH
  * DAMAGE.
**/

using System;
using System.Linq;
using System.Collections.Generic;
using Iodine.Runtime;
using System.Numerics;
using System.Collections;

namespace Iodine.Interop
{
    public class TypeRegistry
    {
        class TypeRegistryEntry
        {
            public readonly TypeMapping Mapping;
            public readonly IodineTypeDefinition IodineType;
            public readonly Type NativeType;

            public TypeRegistryEntry (TypeMapping mapping, IodineTypeDefinition iodineType, Type type)
            {
                Mapping = mapping;
                IodineType = iodineType;
                NativeType = type;
            }
        }

        private List<TypeRegistryEntry> typeMappings = new List<TypeRegistryEntry> ();

        public TypeRegistry ()
        {
            AddTypeMapping (typeof(byte), IodineInteger.TypeDefinition, new Int64TypeMapping ());
            AddTypeMapping (typeof(short), IodineInteger.TypeDefinition, new Int16TypeMapping ());
            AddTypeMapping (typeof(int), IodineInteger.TypeDefinition, new Int32TypeMapping ());
            AddTypeMapping (typeof(long), IodineInteger.TypeDefinition, new Int64TypeMapping ());
            AddTypeMapping (typeof(bool), IodineBool.TypeDefinition, new BoolTypeMapping ());
            AddTypeMapping (typeof(string), IodineString.TypeDefinition, new StringTypeMapping ());
            AddTypeMapping (typeof(float), IodineFloat.TypeDefinition, new FloatTypeMapping ());
            AddTypeMapping (typeof(double), IodineFloat.TypeDefinition, new DoubleTypeMapping ());
            AddTypeMapping (typeof(BigInteger), IodineBigInt.TypeDefinition, new BigIntegerTypeMapping ());
            //AddTypeMapping (null, IodineList.TypeDefinition, new ArrayTypeMapping ());
            //AddTypeMapping (null, IodineTuple.TypeDefinition, new ArrayTypeMapping ());
        }

        public void AddTypeMapping (Type type, IodineTypeDefinition iodineType, TypeMapping mapping)
        {
            typeMappings.Add (new TypeRegistryEntry (mapping, iodineType, type));
        }

        public IodineObject ConvertToIodineObject (object obj)
        {
            if (obj == null) {
                return IodineNull.Instance;
            }

            Type key = obj.GetType ();

            if (key.IsArray) {
                return ConvertFromArray ((object[])obj);
            }

            TypeRegistryEntry entry = typeMappings.Where (p => p.NativeType.IsAssignableFrom (key))
                .FirstOrDefault ();

            if (entry != null) {
                return entry.Mapping.ConvertFrom (this, obj);
            }
            return null;
        }

        private IodineList ConvertFromArray (ICollection collection)
        {
            var enumerator = collection.GetEnumerator ();
            IodineObject [] objects = new IodineObject [collection.Count];
            var i = 0;
            do {
                objects [i++] = ConvertToIodineObject (enumerator.Current);
                if (i == collection.Count)
                    break;
            } while (enumerator.MoveNext ());
            return new IodineList (objects);
        }

        public object ConvertToNativeObject (IodineObject obj)
        {
            if (obj == IodineNull.Instance || obj == null) {
                return null;
            }

            IodineTypeDefinition key = obj.TypeDef;

            TypeRegistryEntry entry = typeMappings.Where (p => p.IodineType == key).FirstOrDefault ();

            if (entry != null) {
                return entry.Mapping.ConvertFrom (this, obj);
            }
            return null;
        }

        public bool TypeMappingExists (IodineTypeDefinition from, Type to)
        {
            if (to.IsArray) {
                return TypeMappingExists (from, to.GetElementType ());
            }
            TypeRegistryEntry entry = typeMappings.Where (p => p.IodineType == from &&
                                      p.NativeType != null &&
                                      p.NativeType.IsAssignableFrom (to)).FirstOrDefault ();
            return entry != null;
        }

        public object ConvertToNativeObject (IodineObject obj, Type expectedType)
        {
            if (obj == IodineNull.Instance || obj == null) {
                return null;
            }

            if (expectedType.IsArray) {

            }

            IodineTypeDefinition key = obj.TypeDef;

            TypeRegistryEntry entry = typeMappings.Where (p => p.IodineType == key &&
                                      p.NativeType != null &&
                                      p.NativeType.IsAssignableFrom (expectedType)).FirstOrDefault ();

            if (entry != null) {
                return entry.Mapping.ConvertFrom (this, obj);
            }
            return null;
        }

        private object[] ConvertToArray (IodineObject obj)
        {
            var list = (IodineList)obj;
            var arr = new object [list.Objects.Count];
            for (var i = 0; i < list.Objects.Count; i++)
                arr [i] = ConvertToNativeObject (list.Objects [i]);
            return arr;
        }
    }
}

