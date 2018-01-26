node('Slave-Azure'){

    stage('Checkout'){
        deleteDir()
        checkout scm
    
    }
    
    
    stage('Deploy Nugget'){
        sh "dotnet pack ./Refit/Refit.csproj --output ${WORKSPACE}"       
        
    }    
    
    stage ("Nexus Publish"){        
        withCredentials([usernamePassword(credentialsId: '4fd86947-a8fc-4122-8217-2674a702b6f6', passwordVariable: 'NEX_Pass', usernameVariable: 'NEX_User')]) {
            sh 'var=$(ls -q *.nupkg) && echo $var > env.txt'           
            int max= sh (returnStdout: true, script: ''' ls -1 *.nupkg | wc -l ''').toInteger()
            for (int i=1; i<=max; ++i) {
                sh "PKG=\$(cat env.txt  | cut -d' ' -f" + i + ") && curl -u $NEX_User:$NEX_Pass -X PUT -v -include -F package=@${WORKSPACE}/\$PKG http://ab669e0edf94311e7a4fa16e33787931-2103240931.us-east-1.elb.amazonaws.com:8081/repository/refit/\$PKG"
            }             
        }
        
    }
    
    stage('Send Message') {               
         slackSend channel: "#jenkins-ci",color: '#0000FF', message: "Build Success: ${env.JOB_NAME} ${env.BUILD_NUMBER}"
   }
} 
