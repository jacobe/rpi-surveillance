var express = require('express'),
	app = express();

var http = require('http').Server(app);
var io = require('socket.io')(http);
var fs = require('fs');
var path = require('path');

var spawn = require('child_process').spawn;
var proc;

app.use('/', express.static(path.join(__dirname, 'stream')));

app.get('/', function (req, res) {
	res.sendFile(__dirname + '/index.html');
});

var sockets = {};

io.on('connection', function (socket) {
	sockets[socket.id] = socket;
	console.log('Total clients connected: ', Object.keys(sockets).length);

	socket.on('disconnect', function() {
		delete sockets[socket.id];
		stopStreaming();
	});

	socket.on('start-stream', function() {
		startStreaming(io);
	});
});

http.listen(80, function() {
	console.log('Listening on port 80');
});

function startStreaming(io) {
	if (app.get('watchingFile')) {
		io.sockets.emit('liveStream', 'image_stream.jpg?_t=' (Math.random() * 100000));
		return;
	}

	var args = ['-w', '640', '-h', '480', '-o', './stream/image_stream.jpg', '-t', '999999999', '-tl', '100'];
	proc = spawn('raspistill', args);

	console.log('Watching for changes...');
	app.set('watchingFile', true);

	fs.watchFile('./stream/image_stream.jpg', function(cur, prev) {
		io.sockets.emit('liveStream', 'image_stream.jpg?_t=' + (Math.random() * 100000));
	});
}

function stopStreaming() {
	if (Object.keys(sockets).length == 0) {
		app.set('watchingFile', false);
		if (proc) proc.kill();
		fs.unwatchFile('./stream/image_stream.jpg');
	}
}