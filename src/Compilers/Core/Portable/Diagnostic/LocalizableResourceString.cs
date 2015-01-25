﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using Roslyn.Utilities;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Microsoft.CodeAnalysis
{
    /// <summary>
    /// A localizable resource string that may possibly be formatted differently depending on culture.
    /// </summary>
    public sealed class LocalizableResourceString : LocalizableString, IObjectReadable, IObjectWritable
    {
        private readonly string _nameOfLocalizableResource;
        private readonly ResourceManager _resourceManager;
        private readonly Type _resourceSource;
        private readonly string[] _formatArguments;
        private static readonly string[] s_emptyArguments = new string[0];

        /// <summary>
        /// Creates a localizable resource string that may possibly be formatted differently depending on culture.
        /// </summary>
        /// <param name="nameOfLocalizableResource">nameof the resource that needs to be localized.</param>
        /// <param name="resourceManager"><see cref="ResourceManager"/> for the calling assembly.</param>
        /// <param name="resourceSource">Type handling assembly's resource management. Typically, this is the static class generated for the resources file from which resources are accessed.</param>
        /// <param name="formatArguments">Optional arguments for formatting the localizable resource string.</param>
        public LocalizableResourceString(string nameOfLocalizableResource, ResourceManager resourceManager, Type resourceSource, params string[] formatArguments)
        {
            if (nameOfLocalizableResource == null)
            {
                throw new ArgumentNullException(nameof(nameOfLocalizableResource));
            }

            if (resourceManager == null)
            {
                throw new ArgumentNullException(nameof(resourceManager));
            }

            if (resourceSource == null)
            {
                throw new ArgumentNullException(nameof(resourceSource));
            }

            if (formatArguments == null)
            {
                throw new ArgumentNullException(nameof(formatArguments));
            }

            _resourceManager = resourceManager;
            _nameOfLocalizableResource = nameOfLocalizableResource;
            _resourceSource = resourceSource;
            _formatArguments = formatArguments;
        }

        private LocalizableResourceString(ObjectReader reader)
        {
            _resourceSource = (Type)reader.ReadValue();
            _nameOfLocalizableResource = reader.ReadString();
            _resourceManager = new ResourceManager(_resourceSource);

            var length = (int)reader.ReadCompressedUInt();
            if (length == 0)
            {
                _formatArguments = s_emptyArguments;
            }
            else
            {
                var argumentsBuilder = ArrayBuilder<string>.GetInstance(length);
                for (int i = 0; i < length; i++)
                {
                    argumentsBuilder.Add(reader.ReadString());
                }

                _formatArguments = argumentsBuilder.ToArrayAndFree();
            }
        }

        Func<ObjectReader, object> IObjectReadable.GetReader()
        {
            return reader => new LocalizableResourceString(reader);
        }

        void IObjectWritable.WriteTo(ObjectWriter writer)
        {
            writer.WriteValue(_resourceSource);
            writer.WriteString(_nameOfLocalizableResource);
            var length = (uint)_formatArguments.Length;
            writer.WriteCompressedUInt(length);
            for (int i = 0; i < length; i++)
            {
                writer.WriteString(_formatArguments[i]);
            }
        }

        public override string ToString(IFormatProvider formatProvider)
        {
            var culture = formatProvider as CultureInfo ?? CultureInfo.CurrentUICulture;
            var resourceString = _resourceManager.GetString(_nameOfLocalizableResource, culture);
            return resourceString != null ?
                (_formatArguments.Length > 0 ? string.Format(resourceString, _formatArguments) : resourceString) :
                string.Empty;
        }
    }
}
