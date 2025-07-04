# Security Setup - Azure Vision API Configuration

## Overview

Secure configuration system using environment variables to protect sensitive Azure Vision API credentials from being committed to version control.

## Quick Setup

1. **Copy the example file:**
   ```bash
   cp .env.example .env
   ```

2. **Edit `.env` with your Azure credentials:**
   ```bash
   # Open in your preferred editor
   nano .env
   # or
   code .env
   ```

3. **Fill in your Azure Vision details:**
   ```env
   AZURE_VISION_ENDPOINT=https://your-resource-name.cognitiveservices.azure.com/
   AZURE_VISION_API_KEY=your-32-character-api-key
   AZURE_VISION_ENABLED=true
   ```

## Getting Azure Vision Credentials

### 1. Create Azure Computer Vision Resource

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource**
3. Search for **Computer Vision**
4. Click **Create**
5. Fill in:
   - **Subscription**: Your Azure subscription
   - **Resource group**: Create new or use existing
   - **Region**: Choose closest to you
   - **Name**: Your resource name (e.g., `my-screenshot-vision`)
   - **Pricing tier**: Choose appropriate tier

### 2. Get Your Credentials

After creation:

1. Go to your Computer Vision resource
2. Click **Keys and Endpoint** in the left menu
3. Copy:
   - **Endpoint**: `https://your-resource-name.cognitiveservices.azure.com/`
   - **Key 1** or **Key 2**: Your API key

## Environment Variables Reference

### Required Variables

```env
# Azure Computer Vision Resource Endpoint
AZURE_VISION_ENDPOINT=https://your-resource-name.cognitiveservices.azure.com/

# Azure Computer Vision API Key (Key 1 or Key 2 from Azure Portal)
AZURE_VISION_API_KEY=a1b2c3d4e5f6g7h8i9j0k1l2m3n4o5p6
```

### Optional Variables

```env
# Enable/disable Azure Vision analysis
AZURE_VISION_ENABLED=true

# Language for analysis results
AZURE_VISION_LANGUAGE=en

# API timeout in seconds  
AZURE_VISION_TIMEOUT_SECONDS=30

# Minimum confidence threshold (0.0 to 1.0)
AZURE_VISION_MIN_CONFIDENCE=0.5
```

## Supported Languages

- `en` - English (default)
- `es` - Spanish  
- `ja` - Japanese
- `pt` - Portuguese
- `zh` - Chinese (Simplified)
- `fr` - French
- `de` - German
- `it` - Italian
- `ko` - Korean

## Configuration Priority

The system loads configuration in this order (later overrides earlier):

1. **appsettings.json** - Default values
2. **Environment variables** - Your `.env` file or system env vars
3. **Command line arguments** - If supported by the application

## Security Features

### ✅ What's Protected

- **API Keys**: Never stored in code or configuration files
- **Endpoints**: Kept in environment variables only  
- **Credentials**: Excluded from version control via `.gitignore`

### ✅ What's Safe to Commit

- **appsettings.json**: Contains only non-sensitive defaults
- **Configuration structure**: No actual secrets
- **Feature flags**: Non-sensitive settings

### ❌ Never Commit

- **`.env` file**: Contains your actual credentials
- **API keys**: In any form
- **Resource endpoints**: With your resource names

## Example Configurations

### Development Setup
```env
AZURE_VISION_ENDPOINT=https://my-dev-vision.cognitiveservices.azure.com/
AZURE_VISION_API_KEY=dev1234567890abcdef1234567890abcd
AZURE_VISION_ENABLED=true
AZURE_VISION_LANGUAGE=en
AZURE_VISION_MIN_CONFIDENCE=0.3
```

### Production Setup
```env
AZURE_VISION_ENDPOINT=https://my-prod-vision.cognitiveservices.azure.com/
AZURE_VISION_API_KEY=prod9876543210fedcba9876543210fedc
AZURE_VISION_ENABLED=true
AZURE_VISION_LANGUAGE=en
AZURE_VISION_MIN_CONFIDENCE=0.7
AZURE_VISION_TIMEOUT_SECONDS=45
```

### Testing/Development (Disabled)
```env
AZURE_VISION_ENABLED=false
```

## Troubleshooting

### Problem: "Azure Vision API returned error 401"
**Solution**: Check your API key
- Verify the key is correct in `.env`
- Try using Key 2 instead of Key 1
- Ensure no extra spaces in the key

### Problem: "Azure Vision API returned error 404"  
**Solution**: Check your endpoint
- Verify the endpoint URL in `.env`
- Ensure it ends with `/` 
- Check the resource name is correct

### Problem: "No .env file found"
**Solution**: Create the file
```bash
cp .env.example .env
# Edit with your credentials
```

### Problem: Environment variables not loading
**Solution**: Check file location
- Ensure `.env` is in the project root
- Restart the application after changes
- Check file permissions

## Verification

### Test Your Setup

1. **Enable Azure Vision:**
   ```env
   AZURE_VISION_ENABLED=true
   ```

2. **Take a screenshot** using the application

3. **Check for `.txt` files** alongside your screenshots

4. **Review logs** for success/error messages

### Example Success Log
```
[12:34:56] Azure Vision analysis completed for screenshot_20240630_123456_abc12345.png
[12:34:56] Using Azure Vision model version: 2023-10-01
[12:34:56] Successfully analyzed image with Azure Vision API 4.0
```

### Example Error Log
```
[12:34:56] Azure Vision API returned error 401: Access denied due to invalid subscription key
```

## Best Practices

### 🔒 Security
- **Rotate API keys** regularly
- **Use different keys** for dev/staging/production
- **Monitor usage** in Azure Portal
- **Set up billing alerts** to prevent unexpected charges

### 🏗️ Development
- **Test with ENABLED=false** when developing UI features
- **Use lower confidence thresholds** for testing
- **Set shorter timeouts** for faster feedback during development

### 🚀 Production
- **Use higher confidence thresholds** for better quality results
- **Monitor API quotas** and usage
- **Set appropriate timeouts** based on your network conditions
- **Enable logging** to track API usage

## Cost Management

### Free Tier Limits
- **20 calls per minute**
- **30,000 calls per month**

### Optimization Tips
- Set `AZURE_VISION_ENABLED=false` during development
- Use higher confidence thresholds to reduce noise
- Monitor usage in Azure Portal
- Consider feature selection to reduce API cost per call

## Support

If you encounter issues:
1. Check the troubleshooting section above
2. Verify your Azure resource is active in Azure Portal  
3. Test with Azure's API explorer to isolate issues
4. Review application logs for detailed error messages

---

**Remember**: Never commit your `.env` file or share your API keys!