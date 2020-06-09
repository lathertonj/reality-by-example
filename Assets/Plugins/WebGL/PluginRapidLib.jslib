

// TODO: this.csArrayToJSArray() may need to be _csArrayToJSArray() (and same for other function)
// TODO: also need to add csArrayToJSArray as a dependency of another function
mergeInto(LibraryManager.library, {
    initializeRapidMix: function ()
    {
        this.currentTrainingID = 0;
        this.trainingSets = {};
        this.currentRegressionID = 0;
        this.regressions = {};
        this.currentClassifierID = 0;
        this.classifiers = {};
        this.rapidLib = window.RapidLib();
    },

    // helper function to turn csharp array pointer and lenght
    // into JS array
    csArrayToJSArray: function ( csArray, csArrayLength )
    {
        var result = [];
        for( var i = 0; i < csArrayLength; i++ )
        {
            // for HEAPF32, it's csArray >> 2
            // I think this turns a byte index into a float index? 32 bits is 4 bytes?
            // and >> 2 divides by four
            // so for HEAPF64, I assume that to turn byte index into a double index, which has 8 bytes,
            // would need to >> 3
            result.push( HEAPF64[(csArray >> 3) + i]);
        }
        return result;
    },

    // helper function to put jsArray onto heap where csharp pointer is
    jsArrayToCSArray: function( jsArray, csArray )
    {
        for( var i = 0; i < jsArray.length; i++ )
        {
            HEAPF64[(csArray >> 3) + i] = jsArray[i];
        }
    },
    
    // private static extern System.UInt32 createEmptyTrainingData();
    createEmptyTrainingData: function ()
    {
        // generate new training ID
        var nextTrainingID = this.currentTrainingID;
        this.currentTrainingID++;
        // training set is an empty list
        this.trainingSets[nextTrainingID] = [];
        return nextTrainingID;
    },
    
    // private static extern System.UInt32 createNewStaticRegression();
    createNewStaticRegression: function ()
    {
        // generate new regression id
        var nextRegressionID = this.currentRegressionID;
        this.currentRegressionID++;
        // generate new regression
        this.regressions[nextRegressionID] = new this.rapidLib.Regression();
        return nextRegressionID;
    },
    
    // private static extern bool recordSingleTrainingElement(
    //     System.UInt32 trainingID,
    //     double[] input, System.UInt32 n_input,
    //     double[] output, System.UInt32 n_ouput
    // );
    recordSingleTrainingElement__deps: ['csArrayToJSArray'],
    recordSingleTrainingElement: function ( trainingID, inputVector, inputVectorLength, outputVector, outputVectorLength )
    {
        if( trainingID in this.trainingSets )
        {
            // convert to JS arrays and add to training set
            this.trainingSets[trainingID].push( { 
                input: _csArrayToJSArray( inputVector, inputVectorLength ),
                output: _csArrayToJSArray( outputVector, outputVectorLength )
            } );
            return true;
        }
        return false;
    },



    // private static extern bool stopPhrase( int trainingID );
    stopPhrase: function( trainingID )
    {
        // nop for now
    },
        
        
    // private static extern bool trainStaticRegression( System.UInt32 regressionID, System.UInt32 trainingID );
    trainStaticRegression: function ( regressionID, trainingID )
    {
        if( regressionID in this.regressions && trainingID in this.trainingSets )
        {
            this.regressions[regressionID].train( this.trainingSets[trainingID] );
            return true;
        }
        return false;
    },


    // private static extern bool runStaticRegression(
    //     System.UInt32 regressionID,
    //     double[] input, System.UInt32 n_input,
    //     double[] output, System.UInt32 n_output
    // );
    runStaticRegression__deps: ['csArrayToJSArray', 'jsArrayToCSArray'],
    runStaticRegression: function ( regressionID, inputVector, inputVectorLength, outputVector, outputVectorLength )
    {
        if( !( regressionID in this.regressions ) )
        {
            return false;
        }
        var input = _csArrayToJSArray( inputVector, inputVectorLength );
        var output = this.regressions[regressionID].run( input );
        if( output.length != outputVectorLength )
        {
            return false;
        }
        // store in heap to "return"
        _jsArrayToCSArray( output, outputVector );
        return true;
    },
    
    
    //     private static extern bool resetStaticRegression( System.UInt32 regressionID );
    resetStaticRegression: function( regressionID ) 
    {
        if( regressionID in this.regressions )
        {
            return this.regressions[regressionID].reset();
        }
        return false;
    },

    //     private static extern bool cleanupTrainingData( System.UInt32 trainingID );
    cleanupTrainingData: function ( trainingID )
    {
        if( trainingID in this.trainingSets )
        {
            this.trainingSets[trainingID] = [];
            return true;
        }
        return false;
    },


    // private static extern System.UInt32 createNewStaticClassifier();
    createNewStaticClassifier: function()
    {
        // generate new classifier id
        var nextClassifierID = this.currentClassifierID;
        this.currentClassifierID++;
        // generate new classifier
        this.classifiers[nextClassifierID] = new this.rapidLib.Classification();
        return nextClassifierID;
    },

    // TODO: will the output be string label, or just int label?
    // private static extern bool recordSingleLabeledTrainingElement(
    //     System.UInt32 trainingID,
    //     double[] input, System.UInt32 n_input,
    //     System.Int32 label
    // );
    recordSingleLabeledTrainingElement__deps: ['csArrayToJSArray'],
    recordSingleLabeledTrainingElement: function ( trainingID, inputVector, inputVectorLength, label )
    {
        if( trainingID in this.trainingSets )
        {
            // convert to JS arrays and add to training set
            this.trainingSets[trainingID].push( { 
                input: _csArrayToJSArray( inputVector, inputVectorLength ),
                output: [label]
            } );
            return true;
        }
        return false;
    },

    // private static extern bool trainStaticClassifier( System.UInt32 classifierID, System.UInt32 trainingID );
    trainStaticClassifier: function( classifierID, trainingID )
    {
        if( classifierID in this.classifiers && trainingID in this.trainingSets )
        {
            this.classifiers[classifierID].train( this.trainingSets[trainingID] );
            return true;
        }
        return false;
    },

    // int label (on desktop, it's string)
    // private static extern System.Int32 runStaticClassifier(
    //     System.UInt32 classifierID,
    //     double[] input, System.UInt32 n_input
    // );
    // TODO: verify whether output is a single-element array
    runStaticClassifier__deps: ['csArrayToJSArray', 'jsArrayToCSArray'],
    runStaticClassifier: function ( classifierID, inputVector, inputVectorLength )
    {
        if( !( classifierID in this.classifiers ) )
        {
            return false;
        }
        var input = _csArrayToJSArray( inputVector, inputVectorLength );
        var output = this.classifiers[classifierID].run( input );
        return output[0];
    },

    // private static extern bool resetStaticClassifier( System.UInt32 classifierID );
    resetStaticClassifier: function( classifierID ) 
    {
        if( classifierID in this.classifiers )
        {
            return this.classifiers[classifierID].reset();
        }
        return false;
    },
  
  });
