//
//  BigRedNoise.h
//  Bacterium
//
//  Created by Giorgi Abelashvili on 3/10/13.
//
//

#import "EnemyAgent.h"
//
@interface BigRedNoise : EnemyAgent
{
    vector <EnemyAgent*> childrens;
    NSTimer *bornChildsTimerValue;
    BOOL bornOfChilds;
    BOOL firstWave;
    BOOL secondWave;
    BOOL thirdWave;
    float prevSpeed;
    int enemiesWavesCounter;
}
@end
