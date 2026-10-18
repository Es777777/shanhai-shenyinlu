mergeInto(LibraryManager.library, {
  ShanHai_Compact: function() { return window.shanhaiViewport().compact ? 1 : 0; },
  ShanHai_Overlay: function(value) { window.shanhaiOverlay=!!value; window.placeFullscreen(); },
  ShanHai_Export: function(name,json) { window.shanhaiFiles.export(UTF8ToString(name),UTF8ToString(json)); },
  ShanHai_Import: function(target) { window.shanhaiFiles.import(UTF8ToString(target)); }
});
