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
   
   
   $("#nav-features").click(function() {
      $("#features").show();
      $("#reviews").hide();
      $("#support").hide();
   });
   
   $("#nav-reviews").click(function() {
      $("#features").hide();
      $("#reviews").show();
      $("#support").hide();
   });
   
   $("#nav-support").click(function() {
      $("#features").hide();
      $("#reviews").hide();
      $("#support").show();
   });

});