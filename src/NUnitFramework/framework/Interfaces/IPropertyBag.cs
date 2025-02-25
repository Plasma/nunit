// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace NUnit.Framework.Interfaces
{
    /// <summary>
    /// A PropertyBag represents a collection of name/value pairs
    /// that allows duplicate entries with the same key. Methods
    /// are provided for adding a new pair as well as for setting
    /// a key to a single value. All keys are strings but values
    /// may be of any type. Null values are not permitted, since
    /// a null entry represents the absence of the key.
    /// 
    /// The entries in a PropertyBag are of two kinds: those that
    /// take a single value and those that take multiple values.
    /// However, the PropertyBag has no knowledge of which entries
    /// fall into each category and the distinction is entirely
    /// up to the code using the PropertyBag.
    /// 
    /// When working with multi-valued properties, client code
    /// should use the Add method to add name/value pairs and
    /// indexing to retrieve a list of all values for a given
    /// key. For example:
    /// 
    ///     bag.Add("Tag", "one");
    ///     bag.Add("Tag", "two");
    ///     Assert.That(bag["Tag"],
    ///       Is.EqualTo(new string[] { "one", "two" }));
    /// 
    /// When working with single-valued properties, client code
    /// should use the Set method to set the value and Get to
    /// retrieve the value. The GetSetting methods may also be
    /// used to retrieve the value in a type-safe manner while
    /// also providing default. For example:
    /// 
    ///     bag.Set("Priority", "low");
    ///     bag.Set("Priority", "high"); // replaces value
    ///     Assert.That(bag.Get("Priority"),
    ///       Is.EqualTo("high"));
    ///     Assert.That(bag.GetSetting("Priority", "low"),
    ///       Is.EqualTo("high"));
    /// </summary>
    public interface IPropertyBag : IXmlNodeBuilder
    {
        /// <summary>
        /// Adds a key/value pair to the property bag
        /// </summary>
        /// <param name="key">The key</param>
        /// <param name="value">The value</param>
        void Add(string key, object value);

        /// <summary>
        /// Sets the value for a key, removing any other
        /// values that are already in the property set.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        void Set(string key, object value);

        /// <summary>
        /// Gets a single value for a key, using the first
        /// one if multiple values are present and returning
        /// null if the value is not found.
        /// </summary>
        object? Get(string key);

        /// <summary>
        /// Gets a flag indicating whether the specified key has
        /// any entries in the property set.
        /// </summary>
        /// <param name="key">The key to be checked</param>
        /// <returns>True if their are values present, otherwise false</returns>
        bool ContainsKey(string key);

        /// <summary>
        /// Tries to retrieve list of values.
        /// </summary>
        /// <param name="key">The key for which the values are to be retrieved</param>
        /// <param name="values">Values, if found</param>
        /// <returns>true if found</returns>
        bool TryGet(string key, [NotNullWhen(true)] out IList? values);

        /// <summary>
        /// Gets or sets the list of values for a particular key, initializes new list behind the key if not found.
        /// </summary>
        /// <param name="key">The key for which the values are to be retrieved or set</param>
        IList this[string key] { get; set; }

        /// <summary>
        /// Gets a collection containing all the keys in the property set
        /// </summary>
        ICollection<string> Keys { get; }
    }
}
