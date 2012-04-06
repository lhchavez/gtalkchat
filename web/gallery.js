$(function() {
   function rotateScreenshots() {
      $("#screenshot").animate(
         {backgroundPosition: "-=240px 0px"},
         500,
         'swing',
         function() { 
            setTimeout(rotateScreenshots, 3000);
         }
      );
   }
   
   if (!$.browser.msie || parseInt($.browser.version, 10) >= 9) {
      setTimeout(rotateScreenshots, 3000);
   }
   
   $("#nav-features").click(function(e) {
      
      $("#reviews").fadeOut(function() {
         $("#support").fadeOut(function() {
            $("#features").fadeIn();
         });
      });
      
      e.preventDefault();
   });
   
   $("#nav-reviews").click(function(e) {
      $("#features").fadeOut(function() {
         $("#support").fadeOut(function() {
            $("#reviews").fadeIn();
         });
      });
      e.preventDefault();
   });
   
   $("#nav-support").click(function(e) {
      $("#features").fadeOut(function() {
         $("#reviews").fadeOut(function() {
            $("#support").fadeIn();
         });
      });
      e.preventDefault();
   });
   
   $("#download").click(function(e) {
      $("#overlay").fadeIn();
      $("#compare").fadeIn();
      
      e.preventDefault();
   });
   
   $("#overlay, #close").click(function() {
      $("#overlay").fadeOut();
      $("#compare").fadeOut();
   });

});