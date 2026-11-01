//
//  TestEnemy1.h
//  Game
//
//  Created by Giorgi Abelashvili on 10/22/11.
//  Copyright 2011 __MyCompanyName__. All rights reserved.
//

#import <Foundation/Foundation.h>
#import "EnemyAgent.h"

@interface RedNoise : EnemyAgent {
    BOOL bornFromBoss;
}
//
@property (nonatomic,assign) BOOL bornFromBoss;
//
@end
