using System;

namespace Simulator.GraphLibrary
{
    public class PairValue<T> : IPairValue<T>

    {
        private readonly T t1;
        private readonly T t2;

        public PairValue(T t1, T t2)
        {
            if (t1 == null || t2 == null)
                throw new ArgumentNullException();
            if (t1.GetType()!= t2.GetType())
                throw new ArgumentException();
            this.t1 = t1;
            this.t2 = t2;
        }
        public bool Contains(T value)
        {
            return value.Equals(t1) || value.Equals(t2);
        }

        public T GetFirst()
        {
            return t1;
        }

        public T GetSecond()
        {
            return t2;
        }

        public override bool Equals(object o)// returns true if object o has the same type (PairValue) and its left and right members are equal to those of the object in consideration
        {
            if (o == null || o.GetType() != typeof(PairValue<T>))
                return false;
            PairValue<T> castedPairValue = (PairValue<T>) o;
            return castedPairValue.t1.Equals(t1) && castedPairValue.t2.Equals(t2);
        }

        public override int GetHashCode() //returns the sum of t1 and t2 hashcodes
        {
            return t1.GetHashCode() + t2.GetHashCode();
        }
    }
}
