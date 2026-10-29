mergeInto(LibraryManager.library, {
 NIU_Notify: function(jsonPointer) {
  try { var payload=JSON.parse(UTF8ToString(jsonPointer)); window.dispatchEvent(new CustomEvent(payload.type,{detail:payload.result})); }
  catch(error) { console.error('NIU event bridge failed',error); }
 }
});
