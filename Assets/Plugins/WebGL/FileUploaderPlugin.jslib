

mergeInto(LibraryManager.library, {
    initializeFileInput: function ( id )
    {
        var uploadInput = document.getElementById( Pointer_stringify( id ) );
        uploadInput.onclick = function( event )
        {
            this.value = null;
        }
        uploadInput.onchange = function( event )
        {
            SendMessage( 'Terrain', 'FileSelected', URL.createObjectURL( event.target.files[0] ) );
        }
    }
});
