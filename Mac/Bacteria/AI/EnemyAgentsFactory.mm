//
//  EnemyAgentsFactory.mm
//  Game
//
//  Created by Giorgi Abelashvili on 10/23/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import "EnemyAgentsFactory.h"
#import "RedNoise.h"
#import "BlueCorosia.h"
#import "Loctopus.h"
#import "SpiralCorosia.h"
#import "Gridium.h"
#import "BigHunterus.h"
#import "BigBlueCorosia.h"
#import "ShootingCorosia.h"
#import "Sunux.h"
#import "Adrenalium.h"
#import "CaterPillar.h"
#import "Chorux.h"
#import "Spawner.h"
#import "Serpentus.h"
#import "LotusBacterius.h"
#import "Tentacle.h"
#import "ZigZagus.h"
#import "ShootingSerpentus.h"
#import "Gnida.h"
#import "ZamriGnida.h"
#import "BigBoss.h"
#import "BigRedNoise.h"
#import "Inferno.h"
#import "Anakonda.h"
#import "Frunctus.h"
#import "ArmagedonBacteria.h"

@implementation EnemyAgentsFactory
//
+(EnemyAgent*) getEnemyWithType:(EnemyAgentType)enemyAgentType withPosition:(CGPoint)position
{
	//
	switch (enemyAgentType)
    {
		case kLoctopus:
			return [[Loctopus alloc] createAtPosition:position];
        case kBlueCorosia:
            return [[BlueCorosia alloc] createAtPosition:position];
        case kRedNoise:
            return [[RedNoise alloc] createAtPosition:position];
        case kSpiralCorosia:
            return [[SpiralCorosia alloc] createAtPosition:position];
        case kGridium:
            return [[Gridium alloc] createAtPosition:position];
        case kBigHunterus:
            return [[BigHunterus alloc] createAtPosition:position];
        case kBigBlueCorosia:
            return [[BigBlueCorosia alloc] createAtPosition:position];
        case kShootingCorosia:
            return [[ShootingCorosia alloc] createAtPosition:position];
        case kSunux:
            return [[Sunux alloc] createAtPosition:position];
        case kAdrenalium:
            return [[Adrenalium alloc] createAtPosition:position];
        case kCaterpillar:
            return [[CaterPillar alloc] createAtPosition:position];
        case kChorux:
            return [[Chorux alloc] createAtPosition:position];
        case kSpawner:
            return [[Spawner alloc] createAtPosition:position];
        case kSerpentus:
            return [[Serpentus alloc] createAtPosition:position];
        case kLotusBacterius:
            return [[LotusBacterius alloc] createAtPosition:position];
        case kTentacle:
            return [[Tentacle alloc] createAtPosition:position];
        case kZigzagus:
            return [[ZigZagus alloc] createAtPosition:position];
        case kShootingSerpentus:
            return [[ShootingSerpentus alloc] createAtPosition:position];
        case kGnida:
            return [[Gnida alloc] createAtPosition:position];
        case kZamriGnida:
            return [[ZamriGnida alloc] createAtPosition:position];
        case kBigBoss:
            return [[BigBoss alloc] createAtPosition:position];
        case kBigRedNoise:
            return [[BigRedNoise alloc] createAtPosition:position];
        case kInferno:
            return [[Inferno alloc] createAtPosition:position];
        case kAnakonda:
            return [[Anakonda alloc] createAtPosition:position];
        case kFrunctus:
            return [[Frunctus alloc] createAtPosition:position];
        /*case kArmagedonBacteria:
            return [[ArmagedonBacteria alloc] createAtPosition:position];*/
        default:
			break;
	}
	//
	return nil;
}
//
@end
