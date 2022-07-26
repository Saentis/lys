/*
Copyright � 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;

namespace Octarine.Lys.Process
{
    public class SingleOperation : IOperationCollection
    {
        public SingleOperation(IOperation operation)
        {
            if (operation == null) throw new ArgumentNullException("operation");
            _operation = operation;
        }

        private IOperation _operation;

        public int Count
        {
            get { return 1; }
        }

        public IOperation Last
        {
            get
            {
                return _operation;
            }
        }
        
        public void Append(IOperation operation)
        {
            throw new NotSupportedException();
        }

        public void Prepend(IOperation operation)
        {
            throw new NotSupportedException();
        }

        public void Append(IOperationCollection collection)
        {
            throw new NotSupportedException();
        }

        public void Prepend(IOperationCollection collection)
        {
            throw new NotSupportedException();
        }

        public void ForEach(Action<IOperation> handler)
        {
            handler(_operation);
        }

        public IOperationCollectionIterator GetIterator()
        {
            return new _Iterator(this);
        }

        private class _Iterator : IOperationCollectionIterator
        {
            public _Iterator(SingleOperation op)
            {
                _op = op;
            }

            private SingleOperation _op;
            private int _index = -1;

            public bool Next()
            {
                return ++_index <= 0;
            }

            public bool Back()
            {
                return --_index >= 0;
            }

            public IOperation? Current
            {
                get
                {
                    if (_index < 0)
                        throw new InvalidOperationException("Call Next() before accessing this.Current");
                    if (_index == 0)
                        return _op._operation;
                    else
                        return null;
                }
            }
        }

    }

}
