export SAS_TOKEN="YOUR-WRITE-ENABLED-SAS-TOKEN"
export STORAGE_ACCOUNT="YOUR-STORAGE-ACCOUNT-KEY"
export TZ='YOUR-TIMEZONE' # eg. 'Europe/Copenhagen'

if [ -n "$1" ]; then
    echo Waiting $1 seconds before snapping a picture...
    sleep $1
fi

./take-and-upload-pic.sh
