### BEGIN INIT INFO
# Provides:          rpi-surveillance
# Required-Start:    $remote_fs $named $syslog
# Required-Stop:     $remote_fs $named $syslog
# Default-Start:     2 3 4 5
# Default-Stop:      0 1 6
# Short-Description: <Short Description>
# Description:       <Longer Description>
### END INIT INFO

#!/bin/sh
#/etc/init.d/node-scripts

export PATH=$PATH:/usr/local/bin
export NODE_PATH=$NODE_PATH:/usr/local/lib/node_modules

case "$1" in
  start)
  sudo -u pi forever start --sourceDir=/home/pi/rpi-livestream -p /home/pi/.forever -a --uid rpi-livestream -c "sudo node" app.js
  ;;
stop)
  sudo -u pi forever stopall
  ;;
*)
  echo "Usage: /etc/init.d/node-scripts {start|stop}"
  exit 1
  ;;
esac

exit 0

