var updateFunc = function (data) {
	if (data.length == 0)
		return;

	var picNumber = data['picNumber'];
	var userNumber = data['userNumber'];
	var latestPics = data['latestPics'];

	$('#picNumber').text(picNumber);
	$('#userNumber').text(userNumber);

	var container = $('#submissions').find('ul');
	latestPics.each (function (key, value) {
		container.append ('<li class="submission"><a href="/i/'+value['id']+'"><ul><li><img src="/Pictures/'+value['id']+'" class="pic"></li><li><span class="tweet">'+value['tweet'].length == 0 ? '<em>(No tweet data to show)</em>' : value['tweet']+'</span></li><li><img class="avatar" src="'+value['avatar']+'"></li></ul></a></li>');
	});
};

$.getJSON('/stats', updateFunc);

(function loop(){
	setTimeout(function(){
		$.getJSON('/stats', updateFunc);
		loop();
	}, 10000);
})();