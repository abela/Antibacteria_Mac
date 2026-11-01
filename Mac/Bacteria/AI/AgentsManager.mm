//
//  AiManager.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/16/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "AgentsManager.h"
#import "EnemyAgent.h"
#import "RedNoise.h"
#import "EnemyAgentsFactory.h"
#import "Utils.h"
#import "SpawnManager.h"
#import "Gridium.h"
#import "AiInput.h"

@implementation AgentsManager
//
@synthesize gameLayer;
//
static AgentsManager *_sharedAgentsManager = nil;
//
+(AgentsManager*)sharedAgentsManager
{
	//
	@synchronized([AgentsManager class])
	{
		if(!_sharedAgentsManager)
			_sharedAgentsManager = [[AgentsManager alloc] init];
	}
	//
	return _sharedAgentsManager;
}
//
//
-(id) init
{
	if((self = [super init]))
	{
		//
		return self;
		//
	}
	return nil;
}
//
-(int) getEnemiesCount
{
    return (int)agents.size();
}
//
-(void) createAgentsCrowd:(int) crowd
{
    //
    float alpha = 1.0f;
    for (int i = 0; i<crowd; i++) {
        CGPoint newPos = [[SpawnManager sharedSpawnManager] getCircleCoordSpawnWithAlpha:alpha];
        EnemyAgentType enemyType = kSpiralCorosia;
        EnemyAgent *testEnemyAgent = [EnemyAgentsFactory getEnemyWithType:enemyType withPosition:newPos];
        agents.push_back(testEnemyAgent);
        alpha+=1;
    }
    //
}
//
-(void) createAgentsCircleCrowd:(int) crowd
{
    //
    float alpha = 1.0f;
    for (int i = 0; i<crowd; i++) {
        CGPoint newPos = [[SpawnManager sharedSpawnManager] getCircleCoordSpawnWithAlpha:alpha];
        EnemyAgentType enemyType = kTentacle;
        EnemyAgent *testEnemyAgent = [EnemyAgentsFactory getEnemyWithType:enemyType withPosition:newPos];
        agents.push_back(testEnemyAgent);
        alpha+=10;
    }
    //
}
//
-(void) createRandomAgentAtPosition:(CGPoint)position :(EnemyAgentType) enemyAgentType
{
    //
    CGPoint newPos = [[SpawnManager sharedSpawnManager] getValidOuterSpawn];
    //
	EnemyAgentType enemyType = enemyAgentType;
	EnemyAgent *testEnemyAgent = [EnemyAgentsFactory getEnemyWithType:enemyType withPosition:newPos];
	agents.push_back(testEnemyAgent);
    //
}
//
-(void) createGridiums:(int) crowd
{
    vector<GridiumSpawn> gridiumSpawn;
    gridiumSpawn = [[SpawnManager sharedSpawnManager] getGridiumPoints:crowd gridStartSpawn:arc4random_uniform(4)];
    for(int i =0;i<crowd;i++)
    {
        EnemyAgentType enemyType = kGridium;
        EnemyAgent *testEnemyAgent = [EnemyAgentsFactory getEnemyWithType:enemyType withPosition:gridiumSpawn[i].startPoint];
        [(Gridium*)testEnemyAgent setEndPos:gridiumSpawn[i].endPoint];
        [(Gridium*)testEnemyAgent setRotAlpha:gridiumSpawn[i].rotAlpha];
        agents.push_back(testEnemyAgent);
    }
}
//
-(void) createRandomEnemiesAtRandomPoints:(int) crowd
{
    //
    for(int i = 0;i < crowd; i++)
    {
        [self createRandomAgentAtPosition:ccp(0,0) :kBlueCorosia];
    }
    //
}
//
-(void) bigExplosion
{
    for(int i =0;i<(int)agents.size();i++)
	{
        if(agents[i].flag == kDefault)
        {
            EnemyAgent *enemyAgent = (EnemyAgent*)agents[i];
            [enemyAgent bigExploded];
        }
    }
}
//
-(void) createOuterEnemiesCrowd:(int)crowd
{
    for(int i = 0;i < crowd; i++)
    {
        CGPoint newPos = [[SpawnManager sharedSpawnManager] getValidOuterSpawn];
        EnemyAgentType enemyType = kSpiralCorosia;
        EnemyAgent *testEnemyAgent = [EnemyAgentsFactory getEnemyWithType:enemyType withPosition:newPos];
        agents.push_back(testEnemyAgent);
    }
}
//
-(void) createRandomEnemiesAroundRandomPoint:(int) crowd
{
    int randX = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] upperLeft].x
                                    to:[[CoreSettings sharedCoreSettings] upperRight].x];
    //
    int randY = [Utils getRandomNumber:[[CoreSettings sharedCoreSettings] bottomRight].y
                                    to:[[CoreSettings sharedCoreSettings] bottomRight].y];
    for(int i = 0;i < crowd; i++)
    {
        CGPoint newPos = [[SpawnManager sharedSpawnManager] getRandomPointAroundPoint:ccp(randX,randY)];
        EnemyAgentType enemyType = kBlueCorosia;
        EnemyAgent *testEnemyAgent = [EnemyAgentsFactory getEnemyWithType:enemyType withPosition:newPos];
        agents.push_back(testEnemyAgent);
    }
}
//
-(void) refreshAgentsManager
{
    for(int i =0;i<(int)agents.size();i++)
	{
        if(agents[i].flag!=kDealloc)
        {
            [(EnemyAgent*)agents[i] refreshEnemy];
        }
    }
}
//
-(void) update
{
	for(int i =0;i<(int)agents.size();i++)
	{
        Agent *agent = agents[i];
		if(agent.flag != kDealloc)
		{
			[agent update];
		}
        else
        {
            [agent releaseAgent];
            [agent release];
            agents.erase(agents.begin() + i);
            break;
        }
    }
    //
}
//
-(void) levelHasWon
{
    [self simulateAgentsManagerRestart];
}
//
-(void) simulateAgentsManagerRestart
{
    for(int i =0;i<(int)agents.size();i++)
	{
        if(agents[i].flag!=kDealloc)
        {
            [agents[i] releaseAgent];
            [agents[i] release];
        }
    }
    agents.clear();
}
//
@end
