using System;
using System.Xml.Serialization;

namespace OpenCover.Framework.Model
{
    /// <summary>
    /// Tracks visit counts for a specific context.
    /// </summary>
    public class ContextVisit
    {
        /// <summary>
        /// Identifier associated with a specific context.
        /// </summary>
        [XmlAttribute("cid")]
        public Guid ContextId { get; set; }

        /// <summary>
        /// Number of visits associated with a specific context.
        /// </summary>
        [XmlAttribute("vc")]
        public uint VisitCount { get; set; }
    }
}

