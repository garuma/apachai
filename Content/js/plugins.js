
// remap jQuery to $
(function($){
	/* MIT License
	 * Paul Irish     | @paul_irish | www.paulirish.com
	 * Andree Hansson | @peolanha   | www.andreehansson.se
	 * 2010.
	 */
	$.event.special.load = {
		add: function (hollaback) {
			if ( this.nodeType === 1 && this.tagName.toLowerCase() === 'img' && this.src !== '' ) {
				// Image is already complete, fire the hollaback (fixes browser issues were cached
				// images isn't triggering the load event)
				if ( this.complete || this.readyState === 4 ) {
					hollaback.handler.apply(this);
				}
				// Check if data URI images is supported, fire 'error' event if not
				else if ( this.readyState === 'uninitialized' && this.src.indexOf('data:') === 0 ) {
					$(this).trigger('error');
				}
				else {
					$(this).bind('load', hollaback.handler);
				}
			}
		}
	};
})(window.jQuery);

// usage: log('inside coolFunc',this,arguments);
// paulirish.com/2009/log-a-lightweight-wrapper-for-consolelog/
window.log = function(){
  log.history = log.history || [];   // store logs to an array for reference
  log.history.push(arguments);
  if(this.console){
    console.log( Array.prototype.slice.call(arguments) );
  }
};

// catch all document.write() calls
(function(){
  var docwrite = document.write;
  document.write = function(q){ 
    log('document.write(): ',q); 
    if (/docwriteregexwhitelist/.test(q)) docwrite(q);  
  }
})();


// background image cache bug for ie6. www.mister-pixel.com/#Content__state=
/*@cc_on   @if (@_win32) { document.execCommand("BackgroundImageCache",false,true) }   @end @*/

