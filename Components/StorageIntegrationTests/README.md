# Storage Integration Tests

## Overview
These integration tests verify the Storage component functionality against actual Azure Blob Storage resources. They use separate test containers to avoid interference with production data.

## Setup Instructions

### 1. Configure User Secrets (Recommended)
To securely store your Azure Storage connection string, use .NET User Secrets:

```bash
# Navigate to the integration test project directory
cd Components/StorageIntegrationTests

# Initialize user secrets (if not already done)
dotnet user-secrets init

# Set your Azure Storage connection string
dotnet user-secrets set "Storage:ConnectionString" "DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
```

### 2. Alternative: Environment Variables
If you prefer environment variables:

```bash
# Set environment variable
export Storage__ConnectionString="DefaultEndpointsProtocol=https;AccountName=YOUR_ACCOUNT;AccountKey=YOUR_KEY;EndpointSuffix=core.windows.net"
```

### 3. Verify Configuration
The tests will automatically skip if no valid connection string is configured. You can verify your setup by running:

```bash
dotnet test --logger "console;verbosity=detailed"
```

## Test Containers

The integration tests use dedicated containers that are separate from production:

- **Screenshots**: `integration-test-screenshots`
- **Thumbnails**: `integration-test-thumbnails`

These containers are automatically created during test execution and can be safely deleted after testing.

## Test Categories

### Storage Operations
- Upload images from byte arrays
- Retrieve image streams
- Generate and access thumbnail URIs
- Delete images and thumbnails

### Image Optimization
- Image resizing and compression
- Thumbnail generation
- Format conversion
- Quality optimization

### Error Handling
- Invalid connection strings
- Missing blobs
- Network failures
- Permission errors

### Performance
- Upload timing measurements
- Compression ratio validation
- Parallel operation verification

## Running Tests

### Run All Integration Tests
```bash
dotnet test
```

### Run with Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run Specific Test Class
```bash
dotnet test --filter "ClassName=AzureStorageIntegrationTests"
```

### Run Tests with Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Data

The tests use generated test images and sample files located in the `TestImages` directory. These include:

- Various image formats (PNG, JPEG, BMP)
- Different image sizes
- Images with transparency
- Invalid/corrupted image data

## Cleanup

### Manual Cleanup
If you need to manually clean up test containers:

```bash
# Using Azure CLI
az storage container delete --name integration-test-screenshots --account-name YOUR_ACCOUNT
az storage container delete --name integration-test-thumbnails --account-name YOUR_ACCOUNT
```

### Automatic Cleanup
The tests include automatic cleanup mechanisms that remove test data after execution.

## Security Best Practices

✅ **DO:**
- Use User Secrets for connection strings
- Use separate storage accounts for testing
- Use dedicated test containers
- Regularly rotate access keys

❌ **DON'T:**
- Commit connection strings to source control
- Use production storage accounts for testing
- Share connection strings in chat or email
- Use overly permissive access policies

## Troubleshooting

### Tests Are Skipped
- Verify your connection string is set in User Secrets
- Check that the connection string format is correct
- Ensure your Azure Storage account is accessible

### Permission Errors
- Verify your connection string has the correct permissions
- Ensure Blob Storage service is enabled
- Check firewall and network access rules

### Network Timeouts
- Check your internet connection
- Verify Azure Storage service status
- Consider increasing timeout values for slow connections

## Configuration Reference

### User Secrets Format
```json
{
  "Storage:ConnectionString": "DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=youraccountkey;EndpointSuffix=core.windows.net"
}
```

### Environment Variable Format
```bash
Storage__ConnectionString="DefaultEndpointsProtocol=https;AccountName=youraccountname;AccountKey=youraccountkey;EndpointSuffix=core.windows.net"
```

### appsettings.json Override
For local development, you can temporarily modify `appsettings.json`, but **never commit connection strings to source control**.

## CI/CD Integration

For automated testing in CI/CD pipelines:

```yaml
# GitHub Actions example
- name: Set Azure Storage Connection String
  env:
    STORAGE_CONNECTION_STRING: ${{ secrets.AZURE_STORAGE_CONNECTION_STRING }}
  run: |
    dotnet user-secrets set "Storage:ConnectionString" "$STORAGE_CONNECTION_STRING" --project Components/StorageIntegrationTests
    
- name: Run Integration Tests
  run: dotnet test Components/StorageIntegrationTests --logger "console;verbosity=detailed"
```

## Next Steps

After setting up your connection string:

1. Run the tests to verify everything works
2. Check the Azure Portal to see the created test containers
3. Review test output for performance metrics
4. Clean up test containers when done

The integration tests will give you confidence that your Storage component works correctly with real Azure resources.