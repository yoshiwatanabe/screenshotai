# Security Checklist - Environment Variables

## ✅ Current Protection Status

### Git Ignore Protection
- ✅ `.env` is in `.gitignore`
- ✅ `.env*` patterns are ignored
- ✅ Verified `.env` is not tracked by git
- ✅ No `.env` files in git history

### File Status Verification
```bash
# ✅ This should show .env is ignored
git check-ignore .env

# ✅ This should show no .env files tracked  
git ls-files | grep "\.env"

# ✅ This should NOT show .env in git status
git status
```

## 🔒 Additional Safety Measures

### 1. Pre-commit Hook (Optional)
Create `.git/hooks/pre-commit` to block .env commits:

```bash
#!/bin/bash
# Check for .env files in staging area
if git diff --cached --name-only | grep -q "\.env$"; then
    echo "❌ ERROR: Attempting to commit .env file!"
    echo "💡 Remove with: git reset HEAD .env"
    exit 1
fi
```

Make it executable:
```bash
chmod +x .git/hooks/pre-commit
```

### 2. Double-Check Commands
Before any git operations:

```bash
# ✅ Safe - check status first
git status

# ✅ Safe - add specific files
git add Components/Storage/src/Services/AzureVisionHttpService.cs

# ❌ DANGEROUS - could add .env
git add .
git add -A
```

### 3. Repository Scan
Verify no secrets in history:
```bash
# Check if .env was ever committed
git log --all --name-only | grep "\.env"

# Should return nothing
```

## 🚨 Emergency Response

### If .env Gets Committed

1. **Stop immediately** - don't push to remote
2. **Remove from staging:**
   ```bash
   git reset HEAD .env
   ```

3. **If already committed locally:**
   ```bash
   git reset --soft HEAD~1  # Undo last commit
   git reset HEAD .env      # Remove .env from staging
   git commit               # Commit without .env
   ```

4. **If pushed to remote:**
   - **Rotate API keys immediately** in Azure Portal
   - Contact team to coordinate history rewrite
   - Consider the keys compromised

### If Secrets Exposed

1. **Immediately rotate credentials:**
   - Go to Azure Portal
   - Generate new API keys
   - Update `.env` with new keys
   - Revoke old keys

2. **Update team:**
   - Notify all developers
   - Update deployment environments
   - Review access logs

## 📋 Regular Security Checks

### Weekly
- [ ] Verify `.env` is not in git status
- [ ] Check no new `.env*` files are tracked
- [ ] Review recent commits for accidental inclusions

### Before Each Commit
- [ ] Run `git status` and review all staged files
- [ ] Ensure no `.env` files in the list
- [ ] Use specific `git add` commands instead of `git add .`

### Before Each Push
- [ ] Double-check `git log --oneline -5` for any suspicious file names
- [ ] Verify Azure API keys are still secure
- [ ] Confirm no team members report credential issues

## 🛡️ Security Best Practices

### Environment Management
- ✅ Use different API keys for dev/staging/production
- ✅ Rotate keys regularly (quarterly)
- ✅ Monitor Azure usage for unusual activity
- ✅ Set up billing alerts in Azure

### Team Collaboration
- ✅ Share `.env.example` (safe template)
- ❌ Never share actual `.env` files
- ❌ Never put credentials in chat/email
- ✅ Use secure credential sharing tools if needed

### Development Workflow
- ✅ Test with `AZURE_VISION_ENABLED=false` during UI development
- ✅ Use separate Azure resources for each environment
- ✅ Document any environment-specific settings
- ❌ Never commit with real credentials "for testing"

## 🔍 Verification Commands

Run these regularly to ensure security:

```bash
# Verify .env protection
echo "Checking .env protection..."
git check-ignore .env && echo "✅ .env is ignored" || echo "❌ .env is NOT ignored!"

# Check for tracked env files  
echo "Checking for tracked .env files..."
tracked_env=$(git ls-files | grep "\.env")
if [ -z "$tracked_env" ]; then
    echo "✅ No .env files are tracked"
else
    echo "❌ WARNING: These .env files are tracked: $tracked_env"
fi

# Verify current status
echo "Current git status:"
git status --porcelain | grep "\.env" && echo "❌ .env files in git status!" || echo "✅ No .env files in git status"
```

## 📞 Emergency Contacts

If credentials are compromised:
1. **Immediate action:** Rotate keys in Azure Portal
2. **Team notification:** Update deployment pipelines  
3. **Documentation:** Update this checklist if new threats discovered

---

**Remember: When in doubt, don't commit. Review first!**