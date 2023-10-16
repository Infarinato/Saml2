﻿using Sustainsys.Saml2.Metadata.Attributes;
using Sustainsys.Saml2.Metadata.Elements;
using Sustainsys.Saml2.Xml;

namespace Sustainsys.Saml2.Metadata;

partial class MetadataSerializer
{
    /// <summary>
    /// Create EntityDescriptor instance. Override to use subclass.
    /// </summary>
    /// <returns>EntityDescriptor</returns>
    protected virtual EntityDescriptor CreateEntityDescriptor() => new();

    /// <summary>
    /// Read an EntityDescriptor
    /// </summary>
    /// <returns>EntityDescriptor</returns>
    public virtual EntityDescriptor ReadEntityDescriptor(XmlTraverser source)
    {
        var entityDescriptor = CreateEntityDescriptor();

        if (source.EnsureName(NamespaceUri, ElementNames.EntityDescriptor))
        {
            ReadAttributes(source, entityDescriptor);
            ReadElements(source.GetChildren(), entityDescriptor);
        }

        source.MoveNext(true);

        ThrowOnErrors(source);

        return entityDescriptor;
    }

    /// <summary>
    /// Read attributes of EntityDescriptor
    /// </summary>
    /// <param name="source">Source data</param>
    /// <param name="entityDescriptor">EntityDescriptor</param>
    protected virtual void ReadAttributes(XmlTraverser source, EntityDescriptor entityDescriptor)
    {
        entityDescriptor.EntityId = source.GetRequiredAbsoluteUriAttribute(AttributeNames.entityID) ?? "";
        entityDescriptor.Id = source.GetAttribute(AttributeNames.ID);
        entityDescriptor.CacheDuraton = source.GetTimeSpanAttribute(AttributeNames.cacheDuration);
        entityDescriptor.ValidUntil = source.GetDateTimeAttribute(AttributeNames.validUntil);
    }

    /// <summary>
    /// Read the child elements of the EntityDescriptor.
    /// </summary>
    /// <param name="source">Source data</param>
    /// <param name="entityDescriptor">Entity Descriptor to populate</param>
    protected virtual void ReadElements(XmlTraverser source, EntityDescriptor entityDescriptor)
    {
        source.MoveNext();

        if (source.ReadAndValidateOptionalSignature(
            TrustedSigningKeys, AllowedHashAlgorithms, out var trustLevel))
        {
            entityDescriptor.TrustLevel = trustLevel;
            source.MoveNext();
        }

        if (source.HasName(NamespaceUri, ElementNames.Extensions))
        {
            entityDescriptor.Extensions = ReadExtensions(source);
            source.MoveNext();
        }

        // Now we're at the actual role descriptors - or possibly an AffiliationDescriptor.
        bool wasRoleDescriptor = true; // Assume the best.
        do
        {
            if(source.EnsureNamespace(NamespaceUri))
            {
                switch (source.CurrentNode?.LocalName)
                {
                    case ElementNames.RoleDescriptor:
                        entityDescriptor.RoleDescriptors.Add(ReadRoleDescriptor(source));
                        break;
                    case ElementNames.IDPSSODescriptor:
                        entityDescriptor.RoleDescriptors.Add(ReadIDPSSODescriptor(source));
                        break;
                    case ElementNames.SPSSODescriptor:
                    case ElementNames.AuthnAuthorityDescriptor:
                    case ElementNames.AttributeAuthorityDescriptor:
                    case ElementNames.PDPDescriptor:
                        source.IgnoreChildren();
                        break;
                    default:
                        wasRoleDescriptor = false; // Nope, something else.
                        break;
                }
            }
        } while (wasRoleDescriptor && source.MoveNext(true));
    }
}