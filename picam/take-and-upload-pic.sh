#!/bin/bash

filename="/ram-pics/latest.jpg"
if [ -n "$1" ]; then
    filename=$1
    echo "Snapping a picture to $filename"
fi
if [ -z "$STORAGE_ACCOUNT" ]; then
    echo "STORAGE_ACCOUNT is not set"
    exit 1
fi
if [ -z "$SAS_TOKEN" ]; then
    echo "SAS_TOKEN is not set"
    exit 1
fi

echo "Snapping a picture..."
raspistill -vf -hf -o $filename

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
echo "Uploading to Azure as $TIMESTAMP.jpg"
curl -X PUT -T $filename -S --max-time 40 -H "x-ms-date: $(date -u)" -H "x-ms-blob-type: BlockBlob" "https://$STORAGE_ACCOUNT.blob.core.windows.net/pics/$TIMESTAMP.jpg?$SAS_TOKEN"

echo "Done"
