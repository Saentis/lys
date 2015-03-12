/*
Copyright © 2015 Steve Muller <steve.muller@outlook.com>
This file is subject to the license terms in the LICENSE file found in the top-level directory of
this distribution and at http://github.com/stevemuller04/lys/blob/master/LICENSE
*/

using System;
using System.Collections.Generic;

namespace Octarine.Lys.Process
{
    public class OperationCollection : IOperationCollection
    {
        public OperationCollection()
        {
        }

        private class Item
        {
            public Item Next;
            public Item Prev;
            public IOperation Object;
        }

        private int _count = 0;
        private Item _first = null, _last = null;

        public int Count
        {
            get { return _count; }
        }

        public IOperation Last
        {
            get
            {
                if (object.ReferenceEquals(_last, null))
                    throw new InvalidOperationException("Collection is empty");
                return _last.Object;
            }
        }

        public void Append(IOperation operation)
        {
            if (operation == null) throw new ArgumentNullException("operation");
            if (_count == 0)
            {
                _first = _last = new Item { Next = null, Prev = null, Object = operation };
            }
            else
            {
                var i = new Item { Next = null, Prev = _last, Object = operation };
                _last.Next = i;
                _last = i;
            }
            _count++;
        }

        public void Prepend(IOperation operation)
        {
            if (operation == null) throw new ArgumentNullException("operation");
            if (_count == 0)
            {
                _first = _last = new Item { Next = null, Prev = null, Object = operation };
            }
            else
            {
                var i = new Item { Next = _first, Prev = null, Object = operation };
                _first.Prev = i;
                _first = i;
            }
            _count++;
        }

        public void Append(IOperationCollection collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (collection is OperationCollection)
            {
                var c = (OperationCollection)collection;
                if (this._count == 0)
                {
                    this._first = c._first;
                    this._last = c._last;
                    this._count = c._count;
                }
                else if (c._count != 0)
                {
                    this._last.Next = c._first;
                    c._first.Prev = this._last;
                    this._last = c._last;
                    this._count += c._count;
                }
            }
            else
            {
                OperationCollection o = new OperationCollection();
                collection.ForEach(o.Append); // leave order as it is
                this.Append(o);
            }
        }

        public void Prepend(IOperationCollection collection)
        {
            if (collection == null) throw new ArgumentNullException("collection");
            if (collection is OperationCollection)
            {
                var c = (OperationCollection)collection;
                if (this._count == 0)
                {
                    this._first = c._first;
                    this._last = c._last;
                    this._count = c._count;
                }
                else if (c._count != 0)
                {
                    c._last.Next = this._first;
                    this._first.Prev = c._last;
                    this._first = c._first;
                    this._count += c._count;
                }
            }
            else
            {
                OperationCollection o = new OperationCollection();
                collection.ForEach(o.Append); // leave order as it is
                this.Prepend(o);
            }
        }

        public void ForEach(Action<IOperation> handler)
        {
            Item pointer = _first;
            while (pointer != null)
            {
                handler(pointer.Object);
                pointer = pointer.Next;
            }
        }

        public IOperationCollectionIterator GetIterator()
        {
            return new _Iterator(this);
        }

        private class _Iterator : IOperationCollectionIterator
        {
            public _Iterator(OperationCollection op)
            {
                _op = op;
                _first = op._first;
                _last = op._last;
                _current = null;
                _state = State.BeforeBeginning;
            }

            private OperationCollection _op;
            private Item _first, _last, _current;
            private State _state;
            private enum State { BeforeBeginning, Active, AfterEnd }

            public bool Next()
            {
                if (_first == null || _last == null)
                    return false;
                switch (_state)
                {
                    case State.BeforeBeginning:
                        _current = _first;
                        _state = State.Active;
                        return true;
                    case State.Active:
                        if ((_current = _current.Next) == null)
                        {
                            _state = State.AfterEnd;
                            return false;
                        }
                        return true;
                    case State.AfterEnd:
                        return false;
                    default:
                        throw new InvalidProgramException();
                }
            }

            public bool Back()
            {
                if (_first == null || _last == null)
                    return false;
                switch (_state)
                {
                    case State.BeforeBeginning:
                        return false;
                    case State.Active:
                        if ((_current = _current.Prev) == null)
                        {
                            _state = State.BeforeBeginning;
                            return false;
                        }
                        return true;
                    case State.AfterEnd:
                        _current = _first;
                        _state = State.Active;
                        return true;
                    default:
                        throw new InvalidProgramException();
                }
            }

            public IOperation Current
            {
                get
                {
                    if (_current == null)
                        return null;
                    else
                        return _current.Object;
                }
            }
        }
    }
}