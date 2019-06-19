if [ -z "$1" ]; then
    echo "No file specified"
    exit 1
fi
if [ -z "$STORAGE_ACCOUNT" ]; then
    echo "STORAGE_ACCOUNT is not set"
    exit 1
fi
if [ -z "$SAS_TOKEN" ]; then
    echo "SAS_TOKEN is not set"
    exit 1
fi
TIMESTAMP=$(date +%Y%m%d-%H%M%S)
curl -X PUT -T $1 -H "x-ms-date: $(date -u)" -H "x-ms-blob-type: BlockBlob" "https://$STORAGE_ACCOUNT.blob.core.windows.net/pics/$TIMESTAMP.jpg?$SAS_TOKEN"
