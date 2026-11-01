//
//  AiManager.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/16/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#include <vector>
//
using namespace std;
@class MainGameLayer;
@class Agent;
//
@interface AgentsManager : NSObject {
	
	MainGameLayer *gameLayer;
	vector <Agent*>  agents;
}
//
@property (nonatomic,retain) MainGameLayer *gameLayer;
//
+(AgentsManager*)sharedAgentsManager;
-(void) update;
-(void) createRandomAgentAtPosition:(CGPoint)position :(EnemyAgentType) enemyAgentType;
-(void) createAgentsCrowd:(int) crowd;
-(void) createAgentsCircleCrowd:(int) crowd;
-(void) createRandomEnemiesAtRandomPoints:(int) crowd;
-(void) createRandomEnemiesAroundRandomPoint:(int) crowd;
-(void) createOuterEnemiesCrowd:(int)crowd;
-(void) createGridiums:(int) crowd;
-(void) refreshAgentsManager;
-(void) simulateAgentsManagerRestart;
-(void) levelHasWon;
-(void) bigExplosion;
-(int) getEnemiesCount;
//
@end
