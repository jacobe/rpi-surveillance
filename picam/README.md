# PiCam

This folder contains the script that snaps and uploads a picture to Azure Blob Storage.

## Installing on a Raspberry Pi

1. Copy `take-and-upload-pic.sh` and `cron-run.sh` to your home folder on the the RaspberryPi.
2. Modify the environment variables in `cron-run.sh` with the correct settings:
   * `STORAGE_ACCOUNT` - the name of the Azure Storage account used to store the uploaded images
   * `SAS_TOKEN` - a valid SAS token (without the preceding '?') - eg. `sv=2018-03-28&ss=b&srt=o&sp=rwlac&se=2019-10-19T13:00:00Z&st=2019-06-19T00:04:29Z&spr=https&sig=a82wKk2BWbdyJvU0xm8%2FEfoXKuRvhQ6EUr1mePY6e28%3D`. A long-lived SAS token can be genreated from the Azure Portal.
   * `TZ` - the local timezone, eg. `Europe/Copenhagen` (for giving the images the correct name, which is the date/time).
3. Use crontab to configure cron to run `cron-run.sh` on regular intervals:
   * Run `crontab -e`
   * Add the following cron expressions to snap a picture every 10 seconds, logging all output to `rpi-surveillance.log` (update the home folder if your user is not called "pi"):

         * * * * * ( cd /home/pi && ./cron-run.sh >> rpi-surveillance.log 2>&1 )
         * * * * * ( cd /home/pi && ./cron-run.sh 10 >> rpi-surveillance.log 2>&1 )
         * * * * * ( cd /home/pi && ./cron-run.sh 20 >> rpi-surveillance.log 2>&1 )
         * * * * * ( cd /home/pi && ./cron-run.sh 30 >> rpi-surveillance.log 2>&1 )
         * * * * * ( cd /home/pi && ./cron-run.sh 40 >> rpi-surveillance.log 2>&1 )
         * * * * * ( cd /home/pi && ./cron-run.sh 50 >> rpi-surveillance.log 2>&1 )
