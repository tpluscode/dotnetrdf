/*

Copyright dotNetRDF Project 2009-12
dotnetrdf-develop@lists.sf.net

------------------------------------------------------------------------

This file is part of dotNetRDF.

dotNetRDF is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

dotNetRDF is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with dotNetRDF.  If not, see <http://www.gnu.org/licenses/>.

------------------------------------------------------------------------

dotNetRDF may alternatively be used under the LGPL or MIT License

http://www.gnu.org/licenses/lgpl.html
http://www.opensource.org/licenses/mit-license.php

If these licenses are not suitable for your intended use please contact
us at the above stated email address to discuss alternative
terms.

*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VDS.RDF.Query.Algebra;
using VDS.RDF.Query.Patterns;
using VDS.RDF.Query.PropertyFunctions;
using VDS.RDF.Update;

namespace VDS.RDF.Query.Optimisation
{
    public class PropertyFunctionOptimiser
        : IAlgebraOptimiser
    {
        public ISparqlAlgebra Optimise(ISparqlAlgebra algebra)
        {
            if (algebra is IBgp)
            {
                IBgp current = (IBgp)algebra;
                if (current.PatternCount == 0) return current;

                List<ITriplePattern> ps = current.TriplePatterns.ToList();
                List<IPropertyFunctionPattern> propFuncs = PropertyFunctionHelper.ExtractPatterns(ps);
                if (propFuncs.Count == 0) return current;

                //Remove raw Triple Patterns pertaining to extracted property functions
                foreach (IPropertyFunctionPattern propFunc in propFuncs)
                {
                    //Track where we need to insert the property function back into the BGP
                    ITriplePattern first = propFunc.OriginalPatterns.First();
                    int origLocation = ps.FindIndex(p => ps.Equals(first));
                    foreach (ITriplePattern tp in propFunc.OriginalPatterns)
                    {
                        int location = ps.FindIndex(p => p.Equals(tp));
                        if (location >= 0)
                        {
                            if (location < origLocation) origLocation--;
                            ps.RemoveAt(location);
                        }
                        else
                        {
                            throw new RdfQueryException("Bad Property Function extraction");
                        }
                    }

                    //Make the insert
                    if (origLocation >= ps.Count)
                    {
                        ps.Add(propFunc);
                    }
                    else
                    {
                        ps.Insert(origLocation, propFunc);
                    }
                }

                //Return a new BGP
                return new Bgp(ps);
            }
            else if (algebra is ITerminalOperator)
            {
                return algebra;
            }
            else if (algebra is IAbstractJoin)
            {
                return ((IAbstractJoin)algebra).Transform(this);
            }
            else if (algebra is IUnaryOperator)
            {
                return ((IUnaryOperator)algebra).Transform(this);
            }
            else
            {
                return algebra;
            }
        }

        public bool IsApplicable(SparqlQuery q)
        {
            return true;
        }

        public bool IsApplicable(SparqlUpdateCommandSet cmds)
        {
            return true;
        }
    }
}
