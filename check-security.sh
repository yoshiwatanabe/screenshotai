#!/bin/bash
# Security verification script for .env protection

echo "🔒 Security Check - Environment Variable Protection"
echo "================================================="
echo

# Check 1: Verify .env is ignored
echo "1. Checking if .env is ignored by git..."
if git check-ignore .env >/dev/null 2>&1; then
    echo "   ✅ .env is properly ignored by git"
else
    echo "   ❌ WARNING: .env is NOT ignored by git!"
    exit 1
fi

# Check 2: Verify no .env files are tracked
echo "2. Checking for tracked .env files..."
tracked_env=$(git ls-files | grep "\.env" || true)
if [ -z "$tracked_env" ]; then
    echo "   ✅ No .env files are tracked by git"
else
    echo "   ❌ WARNING: These .env files are tracked: $tracked_env"
    exit 1
fi

# Check 3: Check current git status for .env files (excluding .env.example)
echo "3. Checking git status for .env files..."
status_env=$(git status --porcelain | grep "\.env$" || true)
if [ -z "$status_env" ]; then
    echo "   ✅ No .env files in git status"
else
    echo "   ❌ WARNING: .env files found in git status:"
    echo "$status_env"
    exit 1
fi

# Check 4: Verify .env.example exists
echo "4. Checking for .env.example template..."
if [ -f ".env.example" ]; then
    echo "   ✅ .env.example template exists"
else
    echo "   ⚠️  WARNING: .env.example template missing"
fi

# Check 5: Verify .gitignore has proper protection
echo "5. Checking .gitignore protection..."
if grep -q "^\.env$" .gitignore; then
    echo "   ✅ .gitignore contains .env protection"
else
    echo "   ❌ WARNING: .gitignore missing .env protection!"
    exit 1
fi

echo
echo "🎉 All security checks passed!"
echo "💡 Your .env file is properly protected from git commits."
echo
echo "📋 Next steps:"
echo "   1. Copy .env.example to .env"
echo "   2. Add your Azure Vision credentials to .env"
echo "   3. Never commit the .env file"
echo
echo "🔍 To run this check again: ./check-security.sh"