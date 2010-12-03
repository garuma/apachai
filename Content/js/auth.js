/* Author: Jérémie Laval
   
*/

$.get ("/RequestTokens", callback = function (data, textStatus, xhr) {
	if (data.length == 0)
		return;

	window.location.replace (data);
}, "text");
