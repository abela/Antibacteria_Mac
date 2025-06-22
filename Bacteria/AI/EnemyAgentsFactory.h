//
//  EnemyAgentsFactory.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/23/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "EnemyAgent.h"

@interface EnemyAgentsFactory : NSObject {

}
+(EnemyAgent*) getEnemyWithType:(EnemyAgentType)enemyAgentType withPosition:(CGPoint)position;
@end
