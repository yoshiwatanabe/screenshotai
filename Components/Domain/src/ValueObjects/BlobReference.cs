namespace ScreenshotManager.Domain.ValueObjects;

public class BlobReference : IEquatable<BlobReference>
{
    public string ContainerName { get; }
    public string BlobName { get; }
    public Uri FullUri { get; }

    public BlobReference(string containerName, string blobName, Uri fullUri)
    {
        if (string.IsNullOrWhiteSpace(containerName))
            throw new ArgumentException("Container name cannot be null or empty", nameof(containerName));
        
        if (string.IsNullOrWhiteSpace(blobName))
            throw new ArgumentException("Blob name cannot be null or empty", nameof(blobName));
        
        if (fullUri == null)
            throw new ArgumentNullException(nameof(fullUri));

        ContainerName = containerName;
        BlobName = blobName;
        FullUri = fullUri;
    }

    public bool Equals(BlobReference? other)
    {
        if (other is null) return false;
        if (ReferenceEquals(this, other)) return true;
        return ContainerName == other.ContainerName && 
               BlobName == other.BlobName && 
               FullUri.Equals(other.FullUri);
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as BlobReference);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ContainerName, BlobName, FullUri);
    }

    public static bool operator ==(BlobReference? left, BlobReference? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BlobReference? left, BlobReference? right)
    {
        return !(left == right);
    }

    public override string ToString()
    {
        return $"{ContainerName}/{BlobName}";
    }
}