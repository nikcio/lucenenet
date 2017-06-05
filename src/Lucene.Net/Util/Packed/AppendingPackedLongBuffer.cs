using System;

namespace Lucene.Net.Util.Packed
{
    /*
     * Licensed to the Apache Software Foundation (ASF) under one or more
     * contributor license agreements.  See the NOTICE file distributed with
     * this work for additional information regarding copyright ownership.
     * The ASF licenses this file to You under the Apache License, Version 2.0
     * (the "License"); you may not use this file except in compliance with
     * the License.  You may obtain a copy of the License at
     *
     *     http://www.apache.org/licenses/LICENSE-2.0
     *
     * Unless required by applicable law or agreed to in writing, software
     * distributed under the License is distributed on an "AS IS" BASIS,
     * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
     * See the License for the specific language governing permissions and
     * limitations under the License.
     */

    /// <summary>
    /// Utility class to buffer a list of signed longs in memory. This class only
    /// supports appending and is optimized for non-negative numbers with a uniform distribution over a fixed (limited) range.
    /// <para/>
    /// NOTE: This was AppendingPackedLongBuffer in Lucene
    /// <para/>
    /// @lucene.internal
    /// </summary>
    public sealed class AppendingPackedInt64Buffer : AbstractAppendingInt64Buffer
    {
        /// <summary>
        /// Initialize a <see cref="AppendingPackedInt64Buffer"/>. </summary>
        /// <param name="initialPageCount">        The initial number of pages. </param>
        /// <param name="pageSize">                The size of a single page. </param>
        /// <param name="acceptableOverheadRatio"> An acceptable overhead ratio per value. </param>
        public AppendingPackedInt64Buffer(int initialPageCount, int pageSize, float acceptableOverheadRatio)
            : base(initialPageCount, pageSize, acceptableOverheadRatio)
        {
        }

        /// <summary>
        /// Create an <see cref="AppendingPackedInt64Buffer"/> with initialPageCount=16,
        /// pageSize=1024 and acceptableOverheadRatio=<see cref="PackedInt32s.DEFAULT"/>.
        /// </summary>
        public AppendingPackedInt64Buffer()
            : this(16, 1024, PackedInt32s.DEFAULT)
        {
        }

        /// <summary>
        /// Create an <see cref="AppendingPackedInt64Buffer"/> with initialPageCount=16,
        /// pageSize=1024.
        /// </summary>
        public AppendingPackedInt64Buffer(float acceptableOverheadRatio)
            : this(16, 1024, acceptableOverheadRatio)
        {
        }

        internal override long Get(int block, int element)
        {
            if (block == valuesOff)
            {
                return pending[element];
            }
            else
            {
                return values[block].Get(element);
            }
        }

        internal override int Get(int block, int element, long[] arr, int off, int len)
        {
            if (block == valuesOff)
            {
                int sysCopyToRead = Math.Min(len, pendingOff - element);
                Array.Copy(pending, element, arr, off, sysCopyToRead);
                return sysCopyToRead;
            }
            else
            {
                /* packed block */
                return values[block].Get(element, arr, off, len);
            }
        }

        internal override void PackPendingValues()
        {
            // compute max delta
            long minValue = pending[0];
            long maxValue = pending[0];
            for (int i = 1; i < pendingOff; ++i)
            {
                minValue = Math.Min(minValue, pending[i]);
                maxValue = Math.Max(maxValue, pending[i]);
            }

            // build a new packed reader
            int bitsRequired = minValue < 0 ? 64 : PackedInt32s.BitsRequired(maxValue);
            PackedInt32s.Mutable mutable = PackedInt32s.GetMutable(pendingOff, bitsRequired, acceptableOverheadRatio);
            for (int i = 0; i < pendingOff; )
            {
                i += mutable.Set(i, pending, i, pendingOff - i);
            }
            values[valuesOff] = mutable;
        }
    }
}