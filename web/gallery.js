$(function() {
   function rotateScreenshots() {
      $("#screenshot").animate(
         {backgroundPosition: "-=240px 0px"},
         500,
         'swing',
         function() { 
            setTimeout(rotateScreenshots, 2000);
         }
      );
   }
    
   setTimeout(rotateScreenshots, 1500);
});