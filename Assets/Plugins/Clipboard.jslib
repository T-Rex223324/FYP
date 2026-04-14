mergeInto(LibraryManager.library, {
    CopyToClipboard: function(text) {
        var str = UTF8ToString(text);
        var tempInput = document.createElement("textarea");
        tempInput.value = str;
        document.body.appendChild(tempInput);
        tempInput.select();
        document.execCommand("copy");
        document.body.removeChild(tempInput);
    }
});